using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using INTERCAL.Compiler;
using INTERCAL.Runtime;
using INTERCAL.Compiler.Exceptions;
using intercal.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class AbstainStatement : Statement
        {
            // One or the other of these will be non-null
            public readonly List<string> Gerunds;
            public readonly string Target;

            public AbstainStatement(Scanner s)
            {
                s.MoveNext();
                if (s.Current.Groups["gerund"].Success)
                {
                    Gerunds = new List<string> { s.Current.Value };
							
                    while (s.PeekNext.Value == "+")
                    {
                        s.MoveNext();
                        s.MoveNext();
                        Gerunds.Add(ReadGroupValue(s,"gerund"));
                    }
                }
                else if (s.Current.Groups["label"].Success)
                {
                    Target = s.Current.Value;
                }
				
                else throw new ParseException($"line {s.LineNumber}: Invalid statement");

            }

            public override void Emit(CompilationContext ctx)
            {
                if (Target != null)
                {
                    var t = ctx.Program[Target].FirstOrDefault();
                    if (t != null)
                    { 
                        Debug.Assert(t.Label == Target);
                        Debug.Assert(t.AbstainSlot >= 0);
                        ctx.EmitRaw($"abstainMap[{t.AbstainSlot}] = false;\n");
                    }
                    else
                    {
                        ctx.Emit($"Lib.Fail(\"{Messages.E139}{Target}\")");
                    }
                }
                else
                {
                    foreach (var r in from t in Gerunds from r in ctx.Program.Statements 
                             where r.GetType() == CompilationContext.AbstainMap[t] select r)
                    {
                        ctx.EmitRaw($"abstainMap[{r.AbstainSlot}] = false;\n");
                    }
                }
            }
        }
    }
}