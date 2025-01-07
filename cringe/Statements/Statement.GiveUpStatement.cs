using INTERCAL.Compiler;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class GiveUpStatement : Statement
    {
        public const string Token = "GIVE UP";
        public const string GerundName = "GIVING UP";
            
        public override void Emit(CompilationContext ctx)
        {
            //-1 means "unconditional return"
            ctx.Emit($"await {Constants.RuntimeGiveUp}();");
            ctx.Emit($"throw new OperationCanceledException();");
        }
    }
}