using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class ComeFromStatement : Statement
        {
            public const string Token = "COME FROM";
            public const string GerundName = "COMING FROM";
            
            public readonly string Target;

            public override void Emit(CompilationContext ctx)
            {
                // We don't have to emit any code - not even this NOPvbecause something will always a COME FROM.
                // Thus all we wind up emitting is a label. We don't actually emit the label here -
                // it gets emitted in Program.EmitStatementProlog so it can integrate with the ABSTAIN / REINSTATE machinery.
            }

            public ComeFromStatement(Scanner s)
            {
                s.MoveNext();
                Target = ReadGroupValue(s, "label");
            }
        }
    }
}