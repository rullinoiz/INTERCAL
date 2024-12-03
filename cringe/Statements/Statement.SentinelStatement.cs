using INTERCAL.Compiler;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public abstract class SentinelStatement : Statement
    {
        // ReSharper disable once UnusedMember.Global
        public string Target = null;

        public override void Emit(CompilationContext ctx) => ctx.Emit("throw new IntercalException(Messages.E633)");
    }
}