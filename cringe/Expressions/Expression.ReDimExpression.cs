using System;
using System.Collections.Generic;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Runtime;

namespace INTERCAL.Expressions
{
    public abstract partial class Expression
    {
        /// <summary>
        /// This is a "special" expression that holds the result of an array redimension. It doesn't return a
        /// <see cref="uint"/> when you evaluate it; instead it returns a string of dimensions. It's similar to
        /// <see cref="Expression.ArrayExpression"/>, except that an <see cref="Expression.ArrayExpression"/> boils down
        /// to a value.
        /// </summary>
        public class ReDimExpression : Expression
        {
            private readonly List<Expression> _dimensions = [];

            public ReDimExpression(Scanner s, Expression first)
            {
                _dimensions.Add(first);

                while (s.PeekNext.Value == "BY")
                {
                    s.MoveNext();
                    s.MoveNext();
                    _dimensions.Add(CreateExpression(s));
                }

            }
			
            public override uint Evaluate(ExecutionContext ctx) => throw new Exception("Don't call this!");

            public int[] GetDimensions(ExecutionContext ctx)
            {
                var retval = new int[_dimensions.Count];
				
                for (var i = 0; i < _dimensions.Count; i++)
                {
                    retval[i] = (int)_dimensions[i].Evaluate(ctx);
                }

                return retval;
            }

            public override void Emit(CompilationContext ctx)
            {
                ctx.EmitRaw("new int[] {");

                var snideRemark = false;
                for (var i = 0; i < _dimensions.Count; i++)
                {
                    //retval[i] = (int)(dimensions[i] as Expression).Evaluate(ctx);
                    ctx.EmitRaw("(int)");
                    _dimensions[i].Emit(ctx);
                    if (_dimensions[i] is ConstantExpression { Value: 0 }) snideRemark = true;
                    
                    if (i < _dimensions.Count - 1)
                        ctx.EmitRaw(",");
                }
                if (snideRemark)
                    Console.WriteLine(CompilationWarning.W239 + $"\n\tON THE WAY TO {Scanner.LineNumber}");

                ctx.EmitRaw("}");
            }

        }
    }
}