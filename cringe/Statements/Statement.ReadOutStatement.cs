using System.Collections.Generic;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Expressions;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class ReadOutStatement : Statement
    {
        public const string Token = "READ OUT";
        public const string GerundName = "READING OUT";
            
        private readonly List<Expression> _lvals = [];

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

                var shortCircuitArray = ae is { Indices: not { Length: not 0 } };

                if (shortCircuitArray)
                {
                    ctx.Emit($"{Constants.RuntimeReadOut}(\"{ae.Name}\");");
                }
                else
                {
                    ctx.EmitRaw(ctx.Indent() + $"{Constants.RuntimeReadOut}(");
                    lval.Emit(ctx);
                    ctx.EmitRaw(");\r\n");
                }
            }
        }
    }
}