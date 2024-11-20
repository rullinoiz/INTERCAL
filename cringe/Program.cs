using System;
using INTERCAL.Runtime;
using INTERCAL.Statements;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using intercal.Compiler.Lexer;

namespace INTERCAL
{
    public class Program
    {
        /// <summary>
        /// A Program maintains a List of <see cref="Statement"/>s.
        /// </summary>
        public readonly List<Statement> Statements = new List<Statement>();

        private IEnumerable<Statement> OccurencesOf(Type t)
        {
            return from s in Statements
                   where s.GetType() == t
                   select s;
        }
        
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

        public IEnumerable<Statement> this[string label] => Statements.Where(s => s.Label == label);

        private Statement this[int n] => Statements[n];


        // TODO: This will need to deal with external calls.
        // A "SafeRecursion" attribute or something would let the runtime query to see if calls out need to be done on a
        // dedicated thread or if the call can be made directly.
        // ReSharper disable once UnusedMember.Local
        private bool IsSimpleFlow(int i, Stack<int> statementsExamined)
        {
            try
            {
                // if we find a cycle just bail out cause that's bad juju
                if (statementsExamined.Contains(i))
                {
                    statementsExamined.ToList().ForEach(Console.WriteLine);
                    Console.WriteLine("Cycle detected encountered at line {0}", Statements[i].LineNumber);

                    return false;
                }

                statementsExamined.Push(i);

                // If trapdoor is true then follow the COME FROM
                // If the statement has a % modifier then we need to ALSO ensure the successor is safe
                if (Statements[i].Trapdoor > 0)
                {
                    var safeTarget = IsSimpleFlow(Statements[i].Trapdoor, statementsExamined);

                    switch (safeTarget)
                    {
                        case true when Statements[i].Percent == 100:
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
                else switch (Statements[i])
                {
                    case Statement.ResumeStatement _ when Statements[i].Percent < 100:
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
                        var target = Statements.FirstOrDefault(s => ns != null && s.Label == ns.Target);
                        if (target == null)
                        {
                            statementsExamined.ToList().ForEach(Console.WriteLine);
                            Console.WriteLine("External call encountered at line {0}", Statements[i].LineNumber);
                            return false;
                        }

                        var isTargetSafe =  IsSimpleFlow(target.StatementNumber, statementsExamined);

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
        /// Can throw <see cref="Messages.E444"/> or <see cref="Messages.E555"/>.
        /// </exception>
        private void FixupComeFroms()
        {
            for (var i = 0; i < Statements.Count; i++)
            {
                if (!(this[i] is Statement.ComeFromStatement s)) continue;
                var sTarget = s.Target;
                var target = this[sTarget].First();

                if (target == null)
                    throw new CompilationException(Messages.E444 + s.Target);

                if (target.Trapdoor < 0)
                    target.Trapdoor = i;
                else
                    throw new CompilationException(Messages.E555 + s.Target);
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

                if (s.Label == null) continue;
                var lblVal = uint.Parse(s.Label.Substring(1, s.Label.Length - 2));
                if (lblVal > ushort.MaxValue)
                {
                    throw new CompilationException(Messages.E197 + " " + s.Label);
                }

                // Check for duplicate label
                if (this[s.Label].Count() > 1)
                    throw new CompilationException(Messages.E182 + " " + s.Label);
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
            ctx.EmitRaw("\r\n//These attributes are used by the compiler for component linking\r\n");

            foreach (var s in Statements.Where(s => s.Label != null)
                         .Where(s => ctx.PublicLabels == null || ctx.PublicLabels[s.Label]))
            {
                ctx.EmitRaw("[assembly: EntryPoint(\"" 
                            + s.Label + "\", \"" + ctx.NameOfAssembly + "\", \"DO_" 
                            + s.Label.Substring(1, s.Label.Length - 2) + "\")]\r\n");
            }
            ctx.EmitRaw("\n\n");

        }

        private void EmitEntryStubs(CompilationContext ctx)
        {
            //We emit a helpful stub for every label (unless the user has suppressed
            //some of them by using "/public:"
            foreach (var s in Statements.Where(s => s.Label != null)
                         .Where(s => ctx.PublicLabels == null || ctx.PublicLabels[s.Label]))
            {
                ctx.EmitRaw("public bool DO_" 
                            + s.Label.Substring(1, s.Label.Length - 2) 
                            + "(ExecutionContext context)\r\n{\r\n");
                ctx.EmitRaw("   return context.Evaluate(Eval," + s.Label + ");\r\n");
                ctx.EmitRaw("}\r\n\r\n");
            }

            // This is the "late bound" version that allows clients to dynamically pass a label. *ALL* labels can be
            // accessed this way.
            if (ctx.TypeOfAssembly != CompilationContext.AssemblyType.Library) return;
            {
                ctx.EmitRaw("   public void DO(ExecutionContext context, string label)\r\n   {\r\n");
                ctx.EmitRaw("      switch(label)\r\n");
                ctx.EmitRaw("      {\r\n");

                foreach (var s in Statements)
                {
                    if (s.Label == null) continue;
                    if ((ctx.PublicLabels != null) &&
                        (ctx.PublicLabels[s.Label] == false))
                    {
                        continue;
                    }

                    var labelValue = uint.Parse(s.Label.Substring(1, s.Label.Length - 2));

                    ctx.EmitRaw("         case \"" + s.Label + "\": ");
                    ctx.EmitRaw("context.Evaluate(Eval," + labelValue + ");  ");
                    ctx.EmitRaw("break;\r\n");
                }

                ctx.EmitRaw("      }\r\n   }\r\n\r\n");
            }
        }

        private void EmitAbstainMap(CompilationContext ctx)
        {
            var abstains = OccurencesOf(typeof(Statement.AbstainStatement)).Count();

            abstains += Statements.Count(s => !s.BEnabled);

            if (abstains <= 0) return;
            {
                // This array holds one entry for every statement that might be abstained, representing an improvement
                // over C-INTERCAL. The following code is a mess and could certainly be refactored.
                ctx.EmitRaw("   bool[] abstainMap = new bool[] {");

                var slot = 0;
                var bfirst = true;
                foreach (var s in Statements.Where(s => !s.BEnabled 
                                                        || _abstainedGerunds.ContainsKey(s.GetType()) 
                                                        || (s.Label != null && _abstainedLabels.ContainsKey(s.Label))))
                {
                    if (!bfirst)
                        ctx.EmitRaw(",");
                    else
                        bfirst = false;

                    ctx.EmitRaw(s.BEnabled ? "true" : "false");
                    s.AbstainSlot = slot;
                    slot++;
                }

                ctx.EmitRaw("};\n\n");
            }
        }

        private void EmitDispatchMap(CompilationContext ctx)
        {
            // If we don't have any labels then we don't need to emit this switch block.
            var labels = from s in Statements
                         where !string.IsNullOrEmpty(s.Label)
                         select s.Label;

            if (!labels.Any()) return;
            //ctx.EmitRaw("dispatch:\n");
            ctx.EmitRaw("   switch(frame.Label)\r\n   {\r\n");

            foreach (var labelNum in from s in Statements 
                     where s.Label != null select int.Parse(s.Label.Substring(1, s.Label.Length - 2)))
            {
                ctx.EmitRaw("      case " + labelNum + ": ");
                ctx.EmitRaw("goto label_" + labelNum + ";\r\n");
            }

            ctx.EmitRaw("   }\r\n");
        }

        private void EmitProgramProlog(CompilationContext ctx)
        {
            ctx.Emit("using System");
            //ctx.Emit("using System.Threading");
            ctx.Emit("using INTERCAL.Runtime");
            ctx.Emit("using System.Diagnostics");

            if (ctx.TypeOfAssembly == CompilationContext.AssemblyType.Library)
                EmitAttributes(ctx);

            // There's nothing on this class that can't be serialized, I don't think
            ctx.EmitRaw("[Serializable]\n");
            ctx.EmitRaw("public class " + ctx.NameOfAssembly + " : " + ctx.BaseClass + "\n{ \n");

            ctx.EmitRaw(
                "   public void Run(){\r\n" +
                "      ExecutionContext ec = INTERCAL.Runtime.ExecutionContext.CreateExecutionContext();\r\n" +
                "      ec.Run(Eval);\r\n" +
                "   }\r\n\r\n");

            // We assume that EXEs do not want to expose labels and that DLLs do.
            if (ctx.TypeOfAssembly == CompilationContext.AssemblyType.Library)
                EmitEntryStubs(ctx);

            EmitAbstainMap(ctx);

            //Now we emit the main function.  Eval overrides the virtual function from the base class.
            ctx.EmitRaw("   protected void Eval(ExecutionFrame frame)" + "   {\r\n");

            EmitDispatchMap(ctx);
        }

        private static void EmitProgramEpilog(CompilationContext ctx)
        {
            ctx.EmitRaw(
            "      //Generic catch-all if the program\r\n" +
            "      throw new Exception(Messages.E633);\r\n\r\n" +
            "   exit:\r\n" +
            "      return;\r\n" +
            "   }\r\n\r\n");

            EmitProperties(ctx);
            ctx.EmitRaw("}\r\n\r\n");

            if (ctx.TypeOfAssembly != CompilationContext.AssemblyType.Exe) return;
            // This enables remoting, such as it is.
            ctx.EmitRaw("class entry\r\n{\n");
            ctx.EmitRaw("   static void Main(string[] args)\r\n{\r\n");
            
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

            ctx.EmitRaw("      //Speed up startup time by ensuring adequate thread availability\r\n");
            ctx.EmitRaw("      System.Threading.ThreadPool.SetMinThreads(80, 4);\r\n\r\n");

            ctx.EmitRaw(
                "      try\r\n" +
                "      {\r\n");
            ctx.EmitRaw($"         var program = new {ctx.NameOfAssembly}();\r\n");
            ctx.EmitRaw(
                "         program.Run();\r\n" +
                "      }\r\n");

            if (ctx.DebugBuild)
            {
                ctx.EmitRaw(
                    "      catch (Exception e)\r\n" +
                    "      {\r\n" +
                    "         Console.WriteLine(e);\r\n" +
                    "      }\r\n"
                );
            }
            else
            {
                ctx.EmitRaw(
                    "      catch (Exception e)\r\n" +
                    "      {\r\n" +
                    "         Console.WriteLine(e.Message);\r\n" +
                    "      }\r\n"
                );

            }


            ctx.EmitRaw("   }\r\n");
            ctx.EmitRaw("}\r\n");
        }

        private static void EmitProperties(CompilationContext c)
        {
            foreach (var s in c.ExternalReferences)
            {
                var fieldName = "m_" + CompilationContext.GeneratePropertyName(s);
                c.EmitRaw("\n");
                c.EmitRaw(s + " " + fieldName + ";\n");
                c.EmitRaw(s + " " + CompilationContext.GeneratePropertyName(s));
                c.EmitRaw("\n{\n");
                c.EmitRaw("   get {");
                c.EmitRaw("if(" + fieldName + "== null) " + fieldName + " = new " + s + "();");
                c.EmitRaw(" return " + fieldName + ";");
                c.EmitRaw("}");
                c.EmitRaw("\n}\n");
            }
        }

        private static void EmitStatementProlog(Statement s, CompilationContext c)
        {
            //if the statement has a label then use its label otherwise
            //we just use "line_<line_number>" 
            //Debug.Assert(s.Label != "(2004)");

            // TODO: convert newlines to spaces otherwise multiline statements will 
            c.EmitRaw("\r\n/* ");
            c.EmitRaw(s.StatementText);

            c.EmitRaw("*/\r\n");

            if (s.Label != null)
                c.EmitRaw("\r\nlabel_" + s.Label.Substring(1, s.Label.Length - 2) + ": \r\n");

            //We need to emit labels for COME FROM so the trapdoor has something to point to.
            else if (s is Statement.ComeFromStatement)
            {
                c.EmitRaw("\r\nline_" + s.StatementNumber + ":\r\n");
            }
            //Uncomment these lines to emit labels for every single statement.  This
            //is not currently necessary..
            //else
            //	c.EmitRaw("\nline_" + s.StatementNumber.ToString() + ":\n");

            //Now we implement E774: RANDOM COMPILER BUG.  The probability of the
            //bug is 1/256 per statement, which is half that of C-Intercal.  See?
            //this compiler is twice as good!
            if (c.Buggy && c.Random.Next(256) == 17)
            {
                c.EmitRaw("//E774: RANDOM COMPILER BUG\r\n");
                c.Emit("Lib.Fail(Messages.E774)");
            }

            //We only emit abstain guards for statements that are the target of 
            //an abstain, either by name or by gerund.
            if (s.AbstainSlot >= 0)
            {
                c.EmitRaw("if(abstainMap[" + s.AbstainSlot.ToString() + "])\n{\n");
            }

            if ((s.Percent > 0) && (s.Percent < 100))
            {
                c.EmitRaw("if(Lib.Rand(100)  < " + s.Percent.ToString() + ")\n{\n");
                c.EmitRaw($"    Trace.WriteLine(\"[{s.StatementNumber:0000}] Rolled the dice and lost.\");");
            }

            if (c.DebugBuild)
            {
                c.EmitRaw($"Trace.WriteLine(\"[{s.StatementNumber:0000}] {s.GetType().Name}\");\n");
            }

        }

        private void EmitStatementEpilog(Statement s, CompilationContext c)
        {
            // COME FROM statements don't include an abstain guard around 
            // the COME FROM itself.  Any checks for abstaining or % prefixes
            // happen as part of processing the trap door below.
            if (!(s is Statement.ComeFromStatement))
            {
                if ((s.Percent < 100) && (s.Percent > 0))
                {
                    c.EmitRaw("}\n\n");
                    c.EmitRaw("else {");
                    c.EmitRaw($"    Trace.WriteLine(\"[{s.StatementNumber:0000}] Rolled the dice and lost.\");");
                    c.EmitRaw("}");
                }

                // Close off the abstain block
                if (s.AbstainSlot >= 0)
                {
                    c.EmitRaw("}\n\n");
                }
            }

            // Now we handle COME FROM.  Note that even if the statement has 
            // been ABSTAINED we still might fall through the trapdoor.  We have to
            // do this even for COME FROM statements in case someone is sick
            // enough to do this:
            //
            // (20) DO COME FROM (10)
            // (30) DO COME FROM (20)
            if (s.Trapdoor <= 0) return;
            var target = Statements[s.Trapdoor];

            // We'll need to emit a label identifying the trapdoor, because if 
            // the line in question is a DO NEXT then when we return from the next
            // we have to evaluate the trapdoor before moving on to the next source line.
            //c.EmitRaw("trapdoor_" + s.StatementNumber + ":\n");

            // make sure the COME FROM in question has not been abstained!
            if (target.AbstainSlot >= 0)
                c.EmitRaw("if(abstainMap[" + target.AbstainSlot + "])\n");

            //If the line is "DO %50 COME FROM" then we should jump 50 percent
            //of the time
            if ((target.Percent > 0) && (target.Percent < 100))
            {
                c.EmitRaw("  if(lib.Rand(100) < " + target.Percent + ")\n   ");
            }

            if (target.Label != null)
                c.EmitRaw("    goto label_" + target.Label.Substring(1, target.Label.Length - 2) + ";\n");
            else
                c.EmitRaw("    goto line_" + target.StatementNumber + ";\n");
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
                {
                    CompilationContext.Warn("(" + s.LineNumber + ") * " + s.StatementText);
                }
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
}
