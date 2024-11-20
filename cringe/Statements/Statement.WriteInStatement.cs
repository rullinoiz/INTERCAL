using System.Collections.Generic;
using INTERCAL.Compiler;
using intercal.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        private class WriteInStatement : Statement
        {
            private readonly List<LValue> _lvals = new List<LValue>();

            public WriteInStatement(Scanner s)
            {
                s.MoveNext();
                _lvals.Add(new LValue(s));
                while (s.PeekNext.Value == "+")
                {
                    s.MoveNext();
                    s.MoveNext();
                    _lvals.Add(new LValue(s));
                }
            }

            public override void Emit(CompilationContext ctx)
            {
                foreach (var lval in _lvals)
                {
                    ctx.Emit("frame.ExecutionContext.WriteIn(\"" + lval.Name + "\")");
                }
            }
        }
    }
}