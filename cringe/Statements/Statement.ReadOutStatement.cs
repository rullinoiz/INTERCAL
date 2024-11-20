using System.Collections.Generic;
using INTERCAL.Compiler;
using intercal.Compiler.Lexer;
using INTERCAL.Expressions;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        private class ReadOutStatement : Statement
        {
            private readonly List<Expression> _lvals = new List<Expression>();

            public ReadOutStatement(Scanner s)
            {
                s.MoveNext();
                _lvals.Add(Expression.CreateExpression(s));
                while (s.PeekNext.Value == "+")
                {
                    s.MoveNext();
                    s.MoveNext();
                    _lvals.Add(Expression.CreateExpression(s));
                }
            }

            public override void Emit(CompilationContext ctx)
            {
                foreach (var lval in _lvals)
                {
                    var ae = lval as Expression.ArrayExpression;

                    var shortCircuitArray = false;

                    if (ae != null)
                    {
                        if (ae.Indices == null || ae.Indices.Length == 0)
                            shortCircuitArray = true;
                    }

                    if (shortCircuitArray)
                    {
                        ctx.EmitRaw($"frame.ExecutionContext.ReadOut(\"{ae.Name}\");");
                    }
                    else
                    {
                        ctx.EmitRaw("frame.ExecutionContext.ReadOut(");
                        lval.Emit(ctx);
                        ctx.EmitRaw(");");
                    }
                }
            }
        }
    }
}