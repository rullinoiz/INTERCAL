using INTERCAL.Compiler;
using intercal.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class RetrieveStatement : StashStatement
        {
            public RetrieveStatement(Scanner s) : base(s){}
            public override void Emit(CompilationContext ctx)
            {
                foreach (var lval in Lvals)
                {
                    ctx.Emit($"Trace.WriteLine(\"       Retrieving {lval.Name}\");");
                    ctx.Emit("frame.ExecutionContext.Retrieve(\"" + lval.Name+ "\")");
                }
            }

        }
    }
}