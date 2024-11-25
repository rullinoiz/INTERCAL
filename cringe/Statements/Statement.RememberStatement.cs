using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class RememberStatement : IgnoreStatement
        {
            public new const string Token = "REMEMBER";
            public new const string GerundName = "REMEMBERING";
            
            public RememberStatement(Scanner s) : base(s) { }

            public override void Emit(CompilationContext ctx)
            {
                foreach (var lval in Targets)
                {
                    ctx.Emit($"frame.ExecutionContext.Remember(\"{lval.Name}\");");
                }
            }

        }
    }
}