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

namespace INTERCAL.Runtime
{ 
    public delegate void IntercalThreadProc(ExecutionFrame context);
    public class ExecutionFrame
    {
        private readonly object _syncLock = new object();
        public readonly ExecutionContext ExecutionContext;
        public readonly IntercalThreadProc Proc;
        private bool _complete;

        /// <summary>
        /// Returned to the calling thread to tell it whether or not it should terminate. Right now <c>true</c> means
        /// "terminate" (you've been forgotten) and false means "continue" (you've been resumed).
        /// </summary>
        private bool _result;
        public readonly int Label;

        public ExecutionFrame(ExecutionContext context, IntercalThreadProc proc, int label)
        {
            ExecutionContext = context;
            Proc = proc;
            Label = label;
        }

        public bool Start()
        {
            // Note that the only reason we need to spin up a new threadis the lurking possiblity of FORGET. If we could
            // guarantee that the function referenced by this.Proc never does a FORGET then we could just make a direct
            // function call.

            var myThread = new IntercalThreadProc(InternalThreadProc);
            myThread.BeginInvoke(this, ar => myThread.EndInvoke(ar), null);

            lock (_syncLock)
            {
                while (!_complete) { Monitor.Wait(_syncLock); }
            }

            return _result;
        }

        public void Resume() => Finish(false);

        public void Abort() => Finish(true);

        private void Finish(bool result)
        {
            _result = result;
            _complete = true;

            lock (_syncLock) { Monitor.Pulse(_syncLock); }
        }
        private void InternalThreadProc(ExecutionFrame ctx)
        {
            try
            {
                Proc(this);
            }
            catch (Exception e)
            {
                ExecutionContext.OnUnhandledException(e);
            }
        }
    }

    public class AsyncDispatcher
    {
        protected bool Done;
        protected readonly object SyncLock = new object();
        protected Exception CurrentException { get; private set; }
        protected readonly Stack<ExecutionFrame> NextingStack = new Stack<ExecutionFrame>();

        protected delegate bool StartProc(IntercalThreadProc proc, int label);
        
        /// <remarks>
        /// Depth must be zero. We depend on the compiler to ensure that resume #0 is ignored as a no-op. The top of the
        /// stack is the frame that is waiting for the current thread to return.
        /// </remarks>
        /// <param name="depth"></param>
        /// <exception cref="IntercalException">Throws <see cref="Messages.E632"/></exception>
        public void Resume(uint depth)
        {
            if (depth <= NextingStack.Count)
            {
                lock (SyncLock)
                {
                    for (var i = 0; i < depth - 1 && NextingStack.Count >= 0; i++)
                    {
                        var f = NextingStack.Pop();

                        //Debug.WriteLine(string.Format("[{0}]   Discarding {1}.{2}({3})\r\n",
                        //Thread.CurrentThread.ManagedThreadId,
                        //f.Proc.Target.GetType().Name,
                        //f.Proc.Method.Name,
                        //f.Label));

                        f.Abort();
                    }

                    var frame = NextingStack.Peek();
                    Debug.WriteLine("[{0}]   Resuming from {1}.{2}({3})\r\n", 
                        Thread.CurrentThread.ManagedThreadId, 
                        frame.Proc.Target.GetType().Name, 
                        frame.Proc.Method.Name, 
                        frame.Label);

                    // Resume the thread that's on top...
                    NextingStack.Peek().Resume();
                    // ..since the thread that's on top has resumed that means nobody is waiting on it anymore.
                    // So we can pop it.
                    NextingStack.Pop();

                    DumpStack();
                }
            }
            else
            {
                throw new IntercalException(Messages.E632);
            }
        }
        public void Forget(int depth)
        {
            lock (SyncLock)
            {
                // Note that it's totally kosher to underflow the nexting stack in intercal. I haven't tested what this
                // code would do if we underflow. 
                for (var i = 0; i < depth && NextingStack.Count > 0; i++)
                {
                    var frame = NextingStack.Pop();
                    frame.Abort();
                }
            }
        }

        public void GiveUp()
        {
            lock (SyncLock)
            {
                while (NextingStack.Count > 0)
                {
                    NextingStack.Pop().Abort();
                }

                Done = true;
                Monitor.Pulse(SyncLock);
            }
        }

        [Conditional("DEBUG")]
        private void DumpStack()
        {
            var sb = new StringBuilder();
            var items = NextingStack.ToList();
            sb.Append($"[{Thread.CurrentThread.ManagedThreadId}] Nexting Stack:\r\n");
            foreach (var frame in items)
                sb.Append($"       {frame.Proc.Target.GetType().Name}.{frame.Proc.Method.Name}({frame.Label})\r\n");
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
                sb.Append($"   at {frame.Proc.Target.GetType().Name}.{frame.Proc.Method.Name}({frame.Label})\r\n");

            CurrentException = new IntercalException(sb.ToString(), e);
            GiveUp();
        }
    }
}