using INTERCAL.Compiler;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class GiveUpStatement : Statement
        {
            public override void Emit(CompilationContext ctx)
            {
                //-1 means "unconditional return"
                ctx.Emit("           frame.ExecutionContext.GiveUp();\r\n");
            }
        }
    }
}