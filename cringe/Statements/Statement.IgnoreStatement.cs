using System.Collections.Generic;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class IgnoreStatement : Statement
        {
            public const string Token = "IGNORE";
            public const string GerundName = "IGNORING";
            
            protected readonly List<LValue> Targets = new List<LValue>();

            public IgnoreStatement(Scanner s)
            {
                while(true)
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
                    ctx.Emit($"{Constants.RuntimeIgnore}(\"{lval.Name}\");");
                }
            }

        }
    }
}