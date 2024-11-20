using INTERCAL.Compiler;
using intercal.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        private class NonsenseStatement : Statement
        {
            public NonsenseStatement(Scanner s)
            {
                LineNumber = s.LineNumber;
                Splatted = true; 
            }

            public override void Emit(CompilationContext ctx)
            {
                // That showoffy jerk Donald Knuth just *had* to put a quote in a multiline comment so now I have to fix those up too.
                var fixedUp = StatementText.Replace("\"", "\\\"").Replace("\r\n", "\" + \r\n\"");
                ctx.EmitRaw("Lib.Fail(\""+ LineNumber + " * " + fixedUp);
                ctx.EmitRaw("\");\n");
            }
        }
    }
}