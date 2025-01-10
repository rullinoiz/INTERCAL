using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Runtime;
using INTERCAL.Statements;

namespace INTERCAL;

public class Program
{
    /// <summary>
    /// A Program maintains a List of <see cref="Statement"/>s.
    /// </summary>
    public readonly List<Statement> Statements = [];

    private IEnumerable<Statement> OccurencesOf(Type t) =>
        from s in Statements
        where s.GetType() == t || (s is Statement.LabelStatement l && l.Statement.GetType() == t)
        select s;

    /// <remarks>
    /// We only need to include the generic <c>ABSTAIN</c> guard for statements that are actually
    /// targets of an <c>ABSTAIN</c>. Whenever we see an <c>ABSTAIN</c> statement or a disabled (<c>NOT</c>)
    /// statement, we put an entry in one of two structures so the generator can emit the <c>ABSTAIN</c> guard.
    /// </remarks>
    private readonly Dictionary<Type, bool> _abstainedGerunds = new Dictionary<Type, bool>();

    /// <summary>
    /// Statements hold indices
    /// </summary>
    private readonly Dictionary<string, bool> _abstainedLabels = new Dictionary<string, bool>();

    //allow the outside world to enumerate statements.
    //public IEnumerator GetEnumerator()
    //{
    //	return Statements.GetEnumerator();
    //}
    public int StatementCount => Statements.Count;

    public IEnumerable<Statement> this[string label] 
        => Statements.Where(s => s is Statement.LabelStatement l && l.Label == label);

    private Statement this[int n] => Statements[n];


    // TODO: This will need to deal with external calls.
    // A "SafeRecursion" attribute or something would let the runtime query to see if calls out need to be done on a
    // dedicated thread or if the call can be made directly.
    // ReSharper disable once UnusedMember.Local
    private bool IsSimpleFlow(int i, Stack<int> statementsExamined)
    {
        try
        {
            var current = Statements[i];
                
            // if we find a cycle just bail out cause that's bad juju
            if (statementsExamined.Contains(i))
            {
                statementsExamined.ToList().ForEach(Console.WriteLine);
                Console.WriteLine("Cycle detected encountered at line {0}", current.LineNumber);

                return false;
            }

            statementsExamined.Push(i);
                
            // If trapdoor is true then follow the COME FROM
            // If the statement has a % modifier then we need to ALSO ensure the successor is safe
            if (current is Statement.LabelStatement { Trapdoor: > 0 } label)
            {
                var safeTarget = IsSimpleFlow(label.Trapdoor, statementsExamined);

                switch (safeTarget)
                {
                    case true when label.Statement.Percent == 100:
                        return true;
                    case true:
                    {
                        var successor = i + 1;
                        return IsSimpleFlow(successor, statementsExamined);
                    }
                    default:
                        return false;
                }
            }
            else switch (current)
            {
                case Statement.ResumeStatement _ when current.Percent < 100:
                {
                    var successor = i + 1;
                    return IsSimpleFlow(successor, statementsExamined);
                }
                case Statement.ResumeStatement _:
                    statementsExamined.ToList().ForEach(Console.WriteLine);
                    Console.WriteLine("RESUME encountered at line {0}", Statements[i].LineNumber);

                    // OOPS. This isn't actually enough. We need to handle depth.  
                    // And from what I've seen so far INTERCAL programs by and large wind up not being safe, so this
                    // optimization is likely not as impactful as I thought. 
                    return true;
                case Statement.GiveUpStatement _ when Statements[i].Percent < 100:
                {
                    var successor = i + 1;
                    return IsSimpleFlow(successor, statementsExamined);
                }
                case Statement.GiveUpStatement _:
                    statementsExamined.ToList().ForEach(Console.WriteLine);
                    Console.WriteLine("GIVE UP encountered at line {0}", Statements[i].LineNumber);
                    return true;
                case Statement.ForgetStatement _:
                    // FORGET is ALWAYS false. Even if it has a percentage attached to it it still introduces the
                    // possibility of a forget.
                    statementsExamined.ToList().ForEach(Console.WriteLine);
                    Console.WriteLine("FORGET encountered at line {0}", Statements[i].LineNumber);
                    return false;
                case Statement.NextStatement _:
                {
                    // A NEXT statement is safe if:
                    // a) the flow beginning at the target is safe
                    // b) the flow of the successor is safe.
                    var ns = Statements[i] as Statement.NextStatement;
                    var target = Statements.FirstOrDefault(s => ns != null && s is Statement.LabelStatement l && l.Label == ns.Target);
                    if (target == null)
                    {
                        statementsExamined.ToList().ForEach(Console.WriteLine);
                        Console.WriteLine("External call encountered at line {0}", Statements[i].LineNumber);
                        return false;
                    }

                    var isTargetSafe = IsSimpleFlow(target.StatementNumber, statementsExamined);

                    if (!isTargetSafe)
                        return false;
                    else
                    {
                        var successor = i + 1;
                        return IsSimpleFlow(successor, statementsExamined);
                    }
                }
                default:
                {
                    var successor = i + 1;
                    return IsSimpleFlow(successor, statementsExamined);
                }
            }
        }
        finally
        {
            statementsExamined.Pop();
        }
    }

    private Program(string input)
    {
        ParseStatements(input);

        FixupComeFroms();

        // Any stray labels will need to get liked up with external libraries somehow.
        // For example, if the program says "DO 1020 NEXT" and is compiling against intercal.runtime.dll
        // then we want to emit an external call to 1020.
        // We don't do anything about it now, but we will in NextStatement.Emit
    }

    #region frontend
    /// <summary>
    /// This function is called after all the statements are parsed.
    /// It walks the list of statements and links up any COME FROM statements
    /// </summary>
    /// <exception cref="CompilationException">
    /// Can throw <see cref="IntercalError.E444"/> or <see cref="IntercalError.E555"/>.
    /// </exception>
    private void FixupComeFroms()
    {
        for (var i = 0; i < Statements.Count; i++)
        {
            if (this[i] is not Statement.ComeFromStatement s) continue;
            var sTarget = s.Target;

            if (this[sTarget].First() is not Statement.LabelStatement target)
                throw new CompilationException(IntercalError.E444 + s.Target);

            if (target.Trapdoor < 0)
                target.Trapdoor = i;
            else
                throw new CompilationException(IntercalError.E555 + s.Target);
        }
    }


    private void ParseStatements(string input)
    {
        var sb = new StringBuilder();

        foreach (var c in input)
        {
            if (c != '!')
                sb.Append(c);
            else
            {
                // replace ! with '.
                sb.Append("'");
                sb.Append('.');
            }
        }

        var src = sb.ToString();
        var scanner = Scanner.CreateScanner(src);
        var begin = scanner.Current.Index;
        var end = begin;

        while (scanner.Current != Match.Empty)
        {
            // begin and end are used to snip out a substring for each statement.
            begin = end;
            var s = Statement.CreateStatement(scanner);
            end = scanner.Current.Index;
            if (end > begin)
                s.StatementText = src.Substring(begin, end - begin).TrimEnd();
            else if (scanner.Current == Match.Empty)
                s.StatementText = src.Substring(begin).TrimEnd();


            s.StatementNumber = StatementCount;
            Statements.Add(s);

            // If this statement abstains a gerund then add the entry to AbstainedGerunds. If this statement
            // abstains a label then add an entry to AbstainedLabels.
            if (s is Statement.AbstainStatement a)
            {
                if (a.Target != null)
                    _abstainedLabels[a.Target] = true;
                else if (a.Gerunds != null)
                {
                    foreach (var t in a.Gerunds.Select(gerund => CompilationContext.AbstainMap[gerund]))
                    {
                        Debug.Assert(t != null);

                        _abstainedGerunds[t] = true;
                    }
                }
            }

            //Debug.Assert(this.OccurencesOf[s.GetType()] != null) ;
            //this.OccurencesOf[s.GetType()] 
            //	= (int)this.OccurencesOf[s.GetType()] + 1;
                
            // TODO: how do we deal with this when we switch OccurencesOf over to a selector instead of a collection? 

            //This was a bug discovered Jan 28, 2003. If a statement is disabled
            //by default then we treat it as an additional abstain.
            //if (!s.bEnabled)
            //	this.OccurencesOf[typeof(Statement.AbstainStatement)]
            //	= (int)this.OccurencesOf[typeof(Statement.AbstainStatement)] + 1;

            if (s is not Statement.LabelStatement lbl) continue;
            var lblVal = lbl.LabelNumber;
            if (lblVal > ushort.MaxValue)
                throw new CompilationException($"{IntercalError.E197} {lbl.Label}");

            if (this[lbl.Label].Count() > 1)
                throw new CompilationException($"{IntercalError.E182} {lbl.Label}");

            // if (s.Label == null) continue;
            // var lblVal = uint.Parse(s.Label.Substring(1, s.Label.Length - 2));
            // if (lblVal > ushort.MaxValue)
            //     throw new CompilationException(Messages.E197 + " " + s.Label);
            //
            // // Check for duplicate label
            // if (this[s.Label].Count() > 1)
            //     throw new CompilationException(Messages.E182 + " " + s.Label);
        }
    }

    public int Politeness
    {
        get
        {
            var please = 0;
            var statements = 0;

            foreach (var s in Statements.Where(s => !(s is Statement.SentinelStatement)))
            {
                statements++;

                if (s.BPlease)
                    please++;
            }

            return (int)((double)please / statements * 100.0);
        }
    }
    #endregion

    #region backend

    private void EmitAttributes(CompilationContext ctx)
    {
        // We emit attributes in the metadata to advertise which programmatic labels this assembly exports.  Future
        // versions of the compiler should have a /hide:<labels> (or something) command to suppress exposing
        // nonpublic lables.

        // Note that we do NOT emit an attribute that can be used to just run a program compiled up.  
        ctx.EmitRaw("\r\n// These attributes are used by the compiler for component linking\r\n");

        foreach (var statement in Statements.Where(s => s is Statement.LabelStatement)
                     .Where(s => ctx.PublicLabels == null || ctx.PublicLabels[((Statement.LabelStatement)s).Label]))
        {
            var s = (Statement.LabelStatement)statement;
            ctx.EmitRaw(
                $"[assembly: {nameof(EntryPointAttribute)}(\"{s.Label}\", nameof({ctx.NameOfAssembly}), nameof({ctx.NameOfAssembly}.DO_{s.Label.Substring(1, s.Label.Length - 2)}))]\r\n");
        }
        ctx.EmitRaw("\r\n");

    }

    private void EmitEntryStubs(CompilationContext ctx)
    {
        //We emit a helpful stub for every label (unless the user has suppressed
        //some of them by using "/public:"
        foreach (var statement in Statements.Where(s => s is Statement.LabelStatement)
                     .Where(s => ctx.PublicLabels == null 
                                 || ctx.PublicLabels[((Statement.LabelStatement)s).Label]))
        {
            var s = (Statement.LabelStatement)statement;
            ctx.EmitRaw(ctx.Indent() + "public Task DO_" 
                                     + s.Label.Substring(1, s.Label.Length - 2) 
                                     + "(ExecutionContext context) => ");
            ctx.EmitRaw($"context.Evaluate(Eval, {s.LabelNumber});\r\n");
        }

        // This is the "late bound" version that allows clients to dynamically pass a label. *ALL* labels can be
        // accessed this way.
        if (ctx.TypeOfAssembly != CompilationContext.AssemblyType.Library) return;
        {
            ctx.Emit("public async Task DO(ExecutionContext context, string label)")
                .BeginBlock()
                .EmitRaw(ctx.Indent() + "switch (label)\r\n")
                .BeginBlock();

            foreach (var statement in Statements)
            {
                if (statement is not Statement.LabelStatement s) continue;
                if (ctx.PublicLabels != null && ctx.PublicLabels[s.Label] == false)
                    continue;

                var labelValue = s.LabelNumber;

                ctx.EmitRaw(ctx.Indent() + $"case \"{s.Label}\":\t");
                ctx.EmitRaw($"await context.Evaluate(Eval, {labelValue});\t");
                ctx.EmitRaw("break;\r\n");
            }

            ctx.EndBlock();
            ctx.EndBlock();
            ctx.EmitRaw("\r\n");
        }
    }

    private void EmitAbstainMap(CompilationContext ctx)
    {
        var abstains = OccurencesOf(typeof(Statement.AbstainStatement)).Count();

        // abstains += Statements.Count(s => !s.BEnabled);

        if (abstains <= 0) return;
        {
            // This array holds one entry for every statement that might be abstained, representing an improvement
            // over C-INTERCAL. The following code is a mess and could certainly be refactored.
            ctx.EmitRaw(ctx.Indent() + $"uint[] {Constants.AbstainMapName} = {{ ");

            var slot = 0;
            var bfirst = true;
            foreach (var s in typeof(Statement).GetNestedTypes()
                         .Where(s => _abstainedGerunds.ContainsKey(s)))
            {
                if (!bfirst)
                    ctx.EmitRaw(", ");
                else
                    bfirst = false;

                ctx.EmitRaw("0");
                Statement.SetStaticAbstainSlot(s, slot);
                slot++;
            }

            foreach (var s in Statements.Where(s => s is Statement.LabelStatement statement 
                                                    && _abstainedLabels.ContainsKey(statement.Label)))
            {
                var label = (Statement.LabelStatement)s;
                if (!bfirst)
                    ctx.EmitRaw(", ");
                else
                    bfirst = false;

                ctx.EmitRaw(label.GetEnabled() ? "0" : "1");
                label.SetAbstainSlot(slot);
                slot++;
            }

            ctx.EmitRaw(" };\r\n");
        }
    }

    private void EmitDispatchMap(CompilationContext ctx)
    {
        // If we don't have any labels then we don't need to emit this switch block.
        var labels = from s in Statements
            where s is Statement.LabelStatement
            select (Statement.LabelStatement)s;

        var labelStatements = labels as Statement.LabelStatement[] ?? labels.ToArray();
        if (labelStatements.Length == 0) return;
        //ctx.EmitRaw("dispatch:\n");
        ctx.Emit("switch (frame.Label)")
            .BeginBlock();

        foreach (var label in labelStatements)
        {
            ctx.EmitRaw(ctx.Indent() + $"case {label.LabelNumber}: ");
            ctx.EmitRaw($"\t\t\tgoto {Constants.NextLabelPrefix}{label.LabelNumber};\r\n");
        }
        ctx.EndBlock();
    }

    private void EmitProgramProlog(CompilationContext ctx)
    {
        ctx.Emit(
            $@"// This code was generated by SICK (Simple INTERCAL Compiler) version {Assembly.GetExecutingAssembly().GetName().Version}, using the command
// cringe.exe {ctx.Arguments.Aggregate((a, b) => a + " " + b)}
// Authorship disclaimed by Jason Whittington 2017. All rights reserved." + "\r\n");
        ctx.Emit("using System;");
        //ctx.Emit("using System.Threading");
        ctx.Emit("using INTERCAL.Runtime;");
        ctx.Emit("using System.Diagnostics;");
        ctx.Emit("using System.Threading.Tasks;");

        if (ctx.TypeOfAssembly == CompilationContext.AssemblyType.Library)
            EmitAttributes(ctx);

        // There's nothing on this class that can't be serialized, I don't think
        ctx.Emit("[Serializable]");
        ctx.Emit($"public class {ctx.NameOfAssembly} : {ctx.BaseClass}");
        ctx.BeginBlock();

        ctx.Emit($"public Task Run() => {nameof(ExecutionContext)}.CreateExecutionContext().Run(Eval);");

        // We assume that EXEs do not want to expose labels and that DLLs do.
        if (ctx.TypeOfAssembly == CompilationContext.AssemblyType.Library)
            EmitEntryStubs(ctx);

        EmitAbstainMap(ctx);

        //Now we emit the main function.  Eval overrides the virtual function from the base class.
        ctx.Emit(Constants.EvalMethodSignature);
        ctx.BeginBlock();

        EmitDispatchMap(ctx);

        if (Statement.TryAgainStatement.TryAgainLine > 0)
            ctx.Emit($"{Constants.ProgramBeginLabel}:");

        // ctx.EndBlock();
    }

    private static void EmitProgramEpilog(CompilationContext ctx)
    {
        if (Statement.TryAgainStatement.TryAgainLine == -1)
        {
            ctx.Emit("// Generic catch-all if the program")
                .Emit($"throw new Exception({nameof(IntercalError)}.{nameof(IntercalError.E633)});");
        }
            
        ctx.Emit($"{Constants.ExitLabelName}:")
            .Emit("return;")
            .EndBlock();

        EmitProperties(ctx);
        ctx.EndBlock();

        if (ctx.TypeOfAssembly != CompilationContext.AssemblyType.Exe) return;
        // This enables remoting, such as it is.
        ctx.Emit(Constants.EntryClassSignature);
        ctx.BeginBlock();
        ctx.Emit(Constants.EntryMethodSignature);
        ctx.BeginBlock();
            
        //var configFileName = ctx.NameOfAssembly + ".exe.config";
        //ctx.EmitRaw("if(System.IO.File.Exists(\"" + configFileName + "\"))\n");
        //ctx.EmitRaw("      if(args.Length >= 1 && args[0].IndexOf(\"/config:\") == 0)");
        //ctx.EmitRaw("         System.Runtime.Remoting.RemotingConfiguration.Configure(\"" + configFileName +  "\");\n\n");

        // Uncomment these three lines if you want the program to pause when it
        // starts up.  This is useful if you want to attach a debugger before
        // the program runs.
        //ctx.EmitRaw("      Console.WriteLine(\"press Enter to run:\");\r\n");
        //ctx.EmitRaw("      Console.ReadLine();\r\n");
        //ctx.EmitRaw("      while(Console.In.Peek() != -1) { Console.Read(); }\r\n\r\n");

        // ctx.Emit("// Speed up startup time by ensuring adequate thread availability");
        // ctx.Emit("var t = System.Threading.ThreadPool.SetMinThreads(80, 4);");

        ctx.Emit("try")
            .BeginBlock()
            .Emit($"var program = new {ctx.NameOfAssembly}();")
            .Emit("await program.Run();")
            .Emit("Console.WriteLine(\"End of program\");")
            // .Emit("await Task.Delay(-1);")
            .EndBlock()
            .Emit("catch (Exception e)")
            .BeginBlock()
            .Emit($"Console.WriteLine(e{(ctx.DebugBuild ? "" : ".Message")});")
            .EndBlock();

        ctx.EndBlock();
        ctx.EndBlock();
    }

    private static void EmitProperties(CompilationContext c)
    {
        foreach (var s in c.ExternalReferences)
        {
            var propertyName = CompilationContext.GeneratePropertyName(s);
            var fieldName = "m_" + propertyName;
            c.EmitRaw("\n");
            c.Emit($"private {s} {fieldName};");
            c.Emit($"private {s} {propertyName} => {fieldName} ??= new {s}();");
            // c.BeginBlock()
            //     .Emit("get")
            //     .BeginBlock()
            //         .Emit($"if ({fieldName} == null) {fieldName} = new {s}();")
            //         .Emit($"return {fieldName};")
            //     .EndBlock()
            // .EndBlock();
        }
    }

    private static void EmitStatementProlog(Statement s, CompilationContext c)
    {
        //if the statement has a label then use its label otherwise
        //we just use "line_<line_number>" 
        //Debug.Assert(s.Label != "(2004)");

        // TODO: convert newlines to spaces otherwise multiline statements will 
        c.Emit($"\r\n{c.Indent()}/* {s.StatementText} */");

        if (s.GetEnabled() == false && s is not Statement.LabelStatement) return;

        switch (s)
        {
            case Statement.LabelStatement label:
                c.Emit($"{Constants.NextLabelPrefix}{label.LabelNumber}:");
                break;
            //We need to emit labels for COME FROM so the trapdoor has something to point to.
            case Statement.ComeFromStatement:
                c.Emit($"{Constants.ComeFromLabelPrefix}{s.StatementNumber}:");
                break;
        }
        
        c.Emit($"Trace.WriteLine(\"[{s.StatementNumber}] {s.StatementText.Replace("\"", "\\\"").Replace("\n", "")}\");");
            
        //Uncomment these lines to emit labels for every single statement.  This
        //is not currently necessary..
        //else
        //	c.EmitRaw("\nline_" + s.StatementNumber.ToString() + ":\n");

        //Now we implement E774: RANDOM COMPILER BUG.  The probability of the
        //bug is 1/256 per statement, which is half that of C-Intercal.  See?
        //this compiler is twice as good!
        if (c.Buggy && c.Random.Next(256) == 17)
        {
            c.Emit("// E774: RANDOM COMPILER BUG");
            c.Emit($"{Constants.LibFail}({nameof(IntercalError)}.{nameof(IntercalError.E774)});");
        }

        // We only emit abstain guards for statements that are the target of 
        // an abstain, either by name or by gerund.
        if (s.GetAbstainSlot() >= 0)
        {
            c.Emit($"if ({Constants.AbstainMapName}[{s.GetAbstainSlot()}] == 0)");
            c.BeginBlock();
        }
        else if (s is Statement.LabelStatement labelStatement && labelStatement.GetAbstainSlot() >= 0)
        {
            c.Emit($"if ({Constants.AbstainMapName}[{labelStatement.GetAbstainSlot()}] == 0)");
            c.BeginBlock();
        }

        if (s.Percent is > 0 and < 100)
        {
            c.Emit($"if ({Constants.LibRand}(100) < {s.Percent})");
            c.BeginBlock()
                .Emit($"Trace.WriteLine(\"[{s.StatementNumber:0000}] Rolled the dice and lost.\");");
            // c.EndBlock();
        }

        if (c.DebugBuild)
            c.Emit($"Trace.WriteLine(\"[{s.StatementNumber:0000}] {s.GetType().Name}\");");
            

    }

    private void EmitStatementEpilog(Statement s, CompilationContext c)
    {
        if (s.GetEnabled() == false && s is not Statement.LabelStatement) return;
            
        // COME FROM statements don't include an abstain guard around 
        // the COME FROM itself.  Any checks for abstaining or % prefixes
        // happen as part of processing the trap door below.
        if (s is not Statement.ComeFromStatement)
        {
            if (s.Percent is < 100 and > 0)
            {
                c.EndBlock();
                c.Emit("else")
                    .BeginBlock()
                    .Emit($"\tTrace.WriteLine(\"[{s.StatementNumber:0000}] Rolled the dice and lost.\");")
                    .EndBlock();
            }

            // Close off the abstain block
            if (s.GetAbstainSlot() >= 0)
            {
                c.EndBlock();
            }
        }

        // Now we handle COME FROM.  Note that even if the statement has 
        // been ABSTAINED we still might fall through the trapdoor.  We have to
        // do this even for COME FROM statements in case someone is sick
        // enough to do this:
        //
        // (20) DO COME FROM (10)
        // (30) DO COME FROM (20)
        if (s is not Statement.LabelStatement { Trapdoor: > 0 } label) return;
        var target = Statements[label.Trapdoor];

        // We'll need to emit a label identifying the trapdoor, because if 
        // the line in question is a DO NEXT then when we return from the next
        // we have to evaluate the trapdoor before moving on to the next source line.
        //c.EmitRaw("trapdoor_" + s.StatementNumber + ":\n");

        // make sure the COME FROM in question has not been abstained!
        if (target.GetAbstainSlot() >= 0)
            c.Emit($"if ({Constants.AbstainMapName}[{target.GetAbstainSlot()}] == 0)");

        //If the line is "DO %50 COME FROM" then we should jump 50 percent
        //of the time
        if (target.Percent is > 0 and < 100)
            c.Emit($"if (lib.Rand(100) < {target.Percent})");
            
        c.Emit(target is Statement.LabelStatement label2
            ? $"goto {Constants.NextLabelPrefix}{label2.LabelNumber};\n"
            : $"goto {Constants.ComeFromLabelPrefix}{target.StatementNumber};");
    }
        
    /// <summary>
    /// This is the master routine for taking a program and emitting it as C#.
    /// </summary>
    /// <param name="c"></param>
    public void EmitCSharp(CompilationContext c)
    {
        EmitProgramProlog(c);

        foreach (var s in Statements)
        {
            if (s is Statement.NextStatement)
            {
                //var stack = new Stack<int>();

                //Console.Write("Examing ({0}) for simple flow...", (s as Statement.NextStatement).Target);
                //bool bSafe = IsSimpleFlow(s.StatementNumber,stack);
                //Console.WriteLine(bSafe);

                // If we determine that the flow is simple then we can
                // optimize away the async call on the NEXTING stack and 
                // replace it with an ordinary function call. Err...handling
                // RESUME n with n>1 might turn out to be painful (but doable). 
            }
                
            EmitStatementProlog(s, c);
            if (s.Splatted)
                CompilationContext.Warn($"({s.LineNumber}) * {s.StatementText}");
            else if (!s.Traditional && c.Traditional)
                throw new CompilationException(IntercalError.E111);
            if (s.GetEnabled() || s is Statement.LabelStatement)
                s.Emit(c);
            EmitStatementEpilog(s, c);
        }

        EmitProgramEpilog(c);
    }

    #endregion

    /// <summary>
    /// Factory method for creating programs.
    /// </summary>
    /// <param name="src">INTERCAL source code.</param>
    /// <returns>A constructed <see cref="Program"/>.</returns>
    public static Program CreateFromString(string src) => new Program(src);

    public static Program CreateFromFile(string file)
    {
        //First we parse statements into the Statements collections
        var srcFile = new StreamReader(file);
        var src = srcFile.ReadToEnd();
        srcFile.Close();

        return new Program(src);
    }
}