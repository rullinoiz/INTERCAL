/*
 * The code in this file implements a thread-based NEXTing stack. This is challenging
   because the sematics of FORGET mean that entries can be dropped from the call stack.
   In an ordinary program a DO NEXT would invoke a subroutine that would always return 
   control back to the parent.  FORGET allows the child to never return, so  DO NEXT / FORGET
   pairs can be used to move control around willy-nilly, (subject to the 80-item max 
   NEXTING depth).  When all code is in a single component a goto-based solution is adequate
   but linking multiple components together is a bigger challenge.
   
   #I deals with this by using a thread-based nexting stack.  When a DO NEXT is encountered 
   the compiler generates a call to ExecutionContext.Evaluate, passing it a delegate referencing 
   a function (typically "Eval()" as well as a label parameter.  This call fires the delegate
   asynchronously on the .NET thread pool and waits for it to complete.  The fuction will 
   eventually either RESUME (returning false) or will FORGET and return true (GIVE UP is basically a fancy FORGET).
   Either of these conditions will release the calling thread which will then continue (in the
   case of RESUME) or immediately exit (in the case of FORGET).  
   
   To help this start up more efficiently progams should call SetMinThreads(80,4) to ensure that 
   80 threads can be made available quickly.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace INTERCAL.Runtime;

public delegate Task IntercalThreadProc(ExecutionFrame context);

public class ExecutionFrame
{
    // private readonly ReentrantAsyncLock.ReentrantAsyncLock _syncLock = new();
    public readonly ExecutionContext ExecutionContext;
    public readonly IntercalThreadProc Proc;
    public CancellationTokenSource Token = new();
    public TaskCompletionSource CompletionToken = new();
    public CancellationToken CancellationToken => Token.Token;
    public Task RunningTask;

    public static int instances;
    
    /// <summary>
    /// Returned to the calling thread to tell it whether or not it should terminate. Right now <c>true</c> means
    /// "terminate" (you've been forgotten) and false means "continue" (you've been resumed).
    /// </summary>
    public readonly int Label;

    public ExecutionFrame(ExecutionContext context, IntercalThreadProc proc, int label)
    {
        ExecutionContext = context;
        Proc = proc;
        Label = label;
        instances++;
    }

    public async Task Start()
    {
        // Note that the only reason we need to spin up a new thread is the lurking possiblity of FORGET. If we could
        // guarantee that the function referenced by this.Proc never does a FORGET then we could just make a direct
        // function call.
        var id = Environment.CurrentManagedThreadId;

        try
        {
            //Trace.WriteLine($"\t[L-{Label} ID-{id}] New task");
            ExecutionContext.Current = this;
            await Task.Run(
                () => Proc(this), 
                Token.Token);
        }
        catch (TaskCanceledException)
        {
            //Trace.WriteLine($"\t[-{Label} ID-{id}] Task cancelled");
        }
        catch (OperationCanceledException)
        {
            //Trace.WriteLine($"\t[L-{Label} ID-{id}] Operation cancelled");
        }
        catch (Exception e)
        {
            ExecutionContext.OnUnhandledException(e);
        }

        //Trace.Write($"\t[L-{Label} ID-{id}] Finished task, ");
        //Trace.Write(Token.Token.IsCancellationRequested ? "aborted\n" : "resumed\n");
        
        // await CompletionToken.Task;
    }

    public void Resume() => Finish(false);

    public void Abort() => Finish(true);

    private void Finish(bool result)
    {
        var id = Task.CurrentId;
        if (!result)
        {
            // Trace.WriteLine($"[L{Label} ID{id}] Resuming");
            ExecutionContext.Current = this;
        }
        else
        {
            // Trace.WriteLine($"\t[L{Label} ID{id}] Canceling token");
            Token.Cancel();
        }
        CompletionToken.SetResult();
        // Trace.WriteLine($"\t[{Label}] setting result returned {CompletionToken.TrySetResult()}");
    }
}

public class AsyncDispatcher
{
    protected bool Done;
    protected readonly ReentrantAsyncLock.ReentrantAsyncLock SyncLock = new();
    protected Exception CurrentException { get; private set; }
    protected readonly Stack<ExecutionFrame> NextingStack = new(80);

    protected delegate bool StartProc(IntercalThreadProc proc, int label);
        
    /// <remarks>
    /// Depth must be zero. We depend on the compiler to ensure that resume #0 is ignored as a no-op. The top of the
    /// stack is the frame that is waiting for the current thread to return.
    /// </remarks>
    /// <exception cref="IntercalException">Throws <see cref="IntercalError.E632"/>.</exception>
    public void Resume(uint depth)
    {
        Trace.WriteLine($"\tResume({depth}); NextingStack.Count = {NextingStack.Count}");
        if (depth <= NextingStack.Count)
        {
            // await using (await SyncLock.LockAsync(CancellationToken.None))
            // {
                for (var i = 0; i < depth - 1 && NextingStack.Count >= 0; i++)
                {
                    var f = NextingStack.Pop();
                    //Trace.WriteLine($"\t[{f.Label}] aborting and popping");

                    // Debug.WriteLine("[{0}]   Discarding {1}.{2}({3})\r\n", Environment.CurrentManagedThreadId, f.Proc.Target?.GetType().Name, f.Proc.Method.Name, f.Label);

                    f.Abort();
                    // tasks.Add(f.RunningTask);
                }
                
                // Resume the thread that's on top...
                NextingStack.Peek().Resume();
                // // ..since the thread that's on top has resumed that means nobody is waiting on it anymore.
                // // So we can pop it.
                NextingStack.Pop();
                //Trace.WriteLine($"\t{NextingStack.Pop().Label}");
                
                DumpStack();
                
                // var t = Task.WhenAll(tasks).ContinueWith(async _ =>
                // {
                //     await using (await SyncLock.LockAsync(CancellationToken.None))
                //     {
                //         
                //     }
                // }).ContinueWith(_ => DumpStack());

                // var frame = NextingStack.Peek();
                // Debug.WriteLine("[{0}]   Resuming from {1}.{2}({3})\r\n",
                //     Environment.CurrentManagedThreadId, 
                //     frame.Proc.Target!.GetType().Name, 
                //     frame.Proc.Method.Name, 
                //     frame.Label);

                
            // }
        }
        else
        {
            throw new IntercalException(IntercalError.E632);
        }

        //return Task.CompletedTask;
    }
        
    public void Forget(int depth)
    {
        Trace.WriteLine($"\tForget({depth}); NextingStack.Count = {NextingStack.Count}");
        // await using (await SyncLock.LockAsync(CancellationToken.None))
        // {
            // Note that it's totally kosher to underflow the nexting stack in intercal. I haven't tested what this
            // code would do if we underflow. 
            for (var i = 0; i < depth && NextingStack.Count > 0; i++)
            {
                var frame = NextingStack.Pop();
                frame.Abort();
            }
            DumpStack();
        // }
        //Trace.WriteLine($"\tEnd forget");
        // return Task.CompletedTask;
    }

    public async Task GiveUp()
    {
        Trace.WriteLine("GIVING UP");
        // await using (await SyncLock.LockAsync(CancellationToken.None))
        // {
            while (NextingStack.Count > 0)
                NextingStack.Pop().Abort();

            Done = true;
        // }
    }

    [Conditional("DEBUG")]
    public void DumpStack()
    {
        // return;
        var sb = new StringBuilder();
        var items = NextingStack.ToList();
        sb.Append($"\t[{Environment.CurrentManagedThreadId}] Nexting Stack:\r\n");
        foreach (var frame in items)
            sb.Append($"\t\t{frame.Proc.Target!.GetType().Name}.{frame.Proc.Method.Name}({frame.Label})\r\n");
        Debug.WriteLine(sb.ToString());
    }
        
    internal void OnUnhandledException(Exception e)
    {
        var sb = new StringBuilder();
        sb.Append(e.Message + "\r\n");
        var list = NextingStack.ToList();
            
        // PLEASE DO NOTE: The topmost label is misleading as that is the label that the most recent DO NEXT jumped
        // to. It is NOT the most recent label executed.
        foreach (var frame in list)
            sb.Append($"\tat {frame.Proc.Target!.GetType().Name}.{frame.Proc.Method.Name}({frame.Label})\r\n");

        throw new IntercalException(sb.ToString(), e);
        // _ = GiveUp();
    }
}