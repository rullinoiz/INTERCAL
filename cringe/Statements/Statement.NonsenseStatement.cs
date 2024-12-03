using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    private class NonsenseStatement : Statement
    {
        public NonsenseStatement()
        {
            LineNumber = Scanner.LineNumber;
            Splatted = true; 
        }

        public override void Emit(CompilationContext ctx)
        {
            // That showoffy jerk Donald Knuth just *had* to put a quote in a multiline comment so now I have to fix those up too.
            var fixedUp = StatementText.Replace("\"", "\\\"").Replace("\r\n", "\" + \r\n\"");
            ctx.Emit($"Lib.Fail(\"{LineNumber} * {fixedUp}\");");
        }
    }
}