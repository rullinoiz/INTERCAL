using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class RetrieveStatement : StashStatement
        {
            public new const string Token = "RETRIEVE";
            public new const string GerundName = "RETRIEVING";
            
            public RetrieveStatement(Scanner s) : base(s) { }
            public override void Emit(CompilationContext ctx)
            {
                var first = true;
                var labelList = string.Empty;
                var labelArgs = string.Empty;
                foreach (var lval in Lvals)
                {
                    if (first)
                    {
                        labelArgs += $"\"{lval.Name}\"";
                        labelList += lval.Name;
                        first = false;
                    }
                    else
                    {
                        labelArgs += $", \"{lval.Name}\"";
                        labelList += " + " + lval.Name;
                    }
                }
                ctx.Emit($"Trace.WriteLine(\"\\tRetrieving {labelList}\");");
                ctx.Emit($"{Constants.RuntimeRetrieve}({labelArgs});");
            }

        }
    }
}