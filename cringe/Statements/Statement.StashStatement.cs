using System.Collections.Generic;
using INTERCAL.Compiler;
using intercal.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class StashStatement : Statement
        {
            protected readonly List<LValue> Lvals = new List<LValue>();

            public StashStatement(Scanner s)
            {
                s.MoveNext();

                var lval = new LValue(s);
                Lvals.Add(lval);

                while (s.PeekNext.Value == "+")
                {
                    s.MoveNext();
                    s.MoveNext();
                    Lvals.Add(new LValue(s));
                }
            }

            public override void Emit(CompilationContext ctx)
            {
                foreach (var lval in Lvals)
                {
                    ctx.Emit($"Trace.WriteLine(\"       Stashing {lval.Name}\");");
                    ctx.Emit("frame.ExecutionContext.Stash(\"" + lval.Name+ "\")");
                }
            }
        }
    }
}