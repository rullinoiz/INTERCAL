using System.Collections.Generic;
using INTERCAL.Compiler;
using intercal.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class IgnoreStatement : Statement
        {
            protected readonly List<LValue> Targets = new List<LValue>();

            public IgnoreStatement(Scanner s)
            {
                for(;;)
                {
                    s.MoveNext();
                    var target = new LValue(s);

                    Targets.Add(target);
                    if (s.PeekNext.Value != "+")
                        break;
                }
            }

            public override void Emit(CompilationContext ctx)
            {
                foreach (var lval in Targets)
                {
                    ctx.Emit("frame.ExecutionContext.Ignore(\"" + lval.Name+ "\")");
                }
            }

        }
    }
}