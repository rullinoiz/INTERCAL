using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class RememberStatement(Scanner s) : IgnoreStatement(s)
    {
        public new const string Token = "REMEMBER";
        public new const string GerundName = "REMEMBERING";

        public override void Emit(CompilationContext ctx)
        {
            foreach (var lval in Targets)
            {
                ctx.Emit($"{Constants.RuntimeRemember}(\"{lval.Name}\");");
            }
        }
    }
}