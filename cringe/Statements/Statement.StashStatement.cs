using System.Collections.Generic;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class StashStatement : Statement
    {
        public const string Token = "STASH";
        public const string GerundName = "STASHING";
            
        protected readonly List<LValue> Lvals = [];

        public StashStatement(Scanner s)
        {
            s.MoveNext();

            var lval = new LValue(s);
            Lvals.Add(lval);

            while (s.PeekNext.Value == "+")
            {
                s.MoveNext();
                s.MoveNext();
                Lvals.Add(new LValue(s));
            }
        }

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
            ctx.Emit($"Trace.WriteLine(\"\\tStashing {labelList}\");");
            ctx.Emit($"{Constants.RuntimeStash}({labelArgs});");
        }
    }
}