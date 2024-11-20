using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using intercal.Compiler.Lexer;
using INTERCAL.Runtime;

namespace INTERCAL.Expressions
{
    internal abstract partial class Expression
    {
        /// <remarks>
        /// This expression might return different types at different times...
        /// </remarks>
        private class BinaryExpression : Expression
        {
            private readonly string _op;
            private Expression _left; 
            private Expression _right;

            public BinaryExpression(Scanner s, string op, Expression left, Expression right)
            {
                _op = op;
                _left = left;
                _right = right;

                switch (op)
                {
                    case "$":
                        ReturnType = typeof(uint);
                        break;
                    case "~":
                        ReturnType = _right.ReturnType;
                        break;
                    case "BY":
                        ReturnType = typeof(uint);
                        break;
                    default:
                        throw new ParseException($"line {s.LineNumber}:Illegal operator {s.Current.Value}");
                }
            }

            protected override Expression Optimize()
            {
                _left = _left.Optimize();
                _right = _right.Optimize();

                if (!(_left is ConstantExpression cleft) || !(_right is ConstantExpression cright)) return this;
                switch (_op)
                {
                    case "$":
                        return new ConstantExpression(Lib.Mingle(cleft.Value, cright.Value));
                    case "~":
                        return new ConstantExpression(Lib.Select(cleft.Value, cright.Value));
                }

                // if both child expression are not constant then we can't fold them
                return this;
            }

            public override uint Evaluate(ExecutionContext ctx) 
            {
                var a = _left.Evaluate(ctx);
                var b = _right.Evaluate(ctx);

                switch (_op)
                {
                    case "$":
                        return Lib.Mingle((ushort)a, (ushort)b);
                    case "~":
                        //A select might use a 16-bit selector to select from a 32-bit
                        //value.  No harm here if we pad the 16-bit value out to 32-bits,
                        //select against the 32-bit value, then take the bottom 16 bits.
                        return Lib.Select(a, b);
                    default:
                        Lib.Fail(null);
                        break;
                }

                return 0;
            }

            public override void Emit(CompilationContext ctx)
            {

                switch (_op)
                {
                    case "$":
                        ctx.EmitRaw("Lib.Mingle(");
                        _left.Emit(ctx);
                        ctx.EmitRaw(", ");
                        _right.Emit(ctx);
                        ctx.EmitRaw(")");
                        break;
                    case "~":
                        //A select might use a 16-bit selector to select from a 32-bit
                        //value.  No harm here if we pad the 16-bit value out to 32-bits,
                        //select against the 32-bit value, then take the bottom 16 bits.
                        ctx.EmitRaw("(uint)Lib.Select(");
                        _left.Emit(ctx);
                        ctx.EmitRaw(",");
                        _right.Emit(ctx);
                        ctx.EmitRaw(")");
                        break;
                    default:
                        Lib.Fail(null);
                        break;
                }
            }

        }
    }
}