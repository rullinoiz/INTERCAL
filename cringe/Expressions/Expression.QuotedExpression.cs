using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Runtime;
using INTERCAL.Statements;

namespace INTERCAL.Expressions
{
    public abstract partial class Expression
    {
        private class QuotedExpression : Expression
        {
            private readonly string _unaryOp;
			
            private Expression _child;

            public QuotedExpression(Scanner s)
            {
                var delimeter = s.Current.Value;

                s.MoveNext();
                if (s.Current.Groups["unary_op"].Success)
                {
                    // 3.4.3 A unary operator is applied to a sparked or rabbit-eared 
                    // expression by inserting the operator immediately following the opening spark or ears
                    _unaryOp = s.Current.Value;
                    s.MoveNext();
                }

                // We're alerting children that when they see a delimeter to return it to
                // us, not try to consume it for a new quoted expression.  This only matters
                // for arrays, where statements like:
                //	DO .1 <- ',4SUB#1'$',4SUB#2'

                _child = CreateExpression(s, delimeter);

                s.MoveNext();

                Statement.AssertToken(s, delimeter);

                ReturnType = _child.ReturnType;
            }

            protected override Expression Optimize() 
            {
                // first we optimize the child.
                _child = _child.Optimize();

                // if the child expression is constant then we can just compile-time evaluate our operator and return
                // a constant expression.
                if (_child is not ConstantExpression c) return this;
                if (_unaryOp == null)
                    return _child;
				
                if (c.Value < ushort.MaxValue)
                {
                    var tmp = (ushort)c.Value;
                    switch (_unaryOp)
                    {
                        case "v": 
                        case "V":
                            return new ConstantExpression(Lib.UnaryOr16(tmp));
                        case "&": 
                            return new ConstantExpression(Lib.UnaryAnd16(tmp));
                        case "?": 
                            return new ConstantExpression(Lib.UnaryXor16(tmp));
                    }
                }
                else
                {
                    switch (_unaryOp)
                    {
                        case "v": 
                        case "V":
                            return new ConstantExpression(Lib.UnaryOr32(c.Value));
                        case "&": 
                            return new ConstantExpression(Lib.UnaryAnd32(c.Value));
                        case "?": 
                            return new ConstantExpression(Lib.UnaryXor32(c.Value));
                    }
                }

                // if the child expression was not constant then we can't optimize.
                return this; 
            }

            public override void Emit(CompilationContext ctx)
            {
                ctx.EmitRaw("(");
                if (_unaryOp == null)
                    _child.Emit(ctx);
                else
                {
                    switch (_unaryOp)
                    {
                        case "v": 
                        case "V":
                            ctx.EmitRaw($"{Constants.LibOr}(");
                            _child.Emit(ctx);
                            ctx.EmitRaw(")");
                            break;
                        case "&":
                            ctx.EmitRaw($"{Constants.LibAnd}(");
                            _child.Emit(ctx);
                            ctx.EmitRaw(")");
                            break;
                        case "?":
                            ctx.EmitRaw($"{Constants.LibXor}(");
                            _child.Emit(ctx);
                            ctx.EmitRaw(")");
                            break;
                    }
                }

                ctx.EmitRaw(")");
            }

            public override uint Evaluate(ExecutionContext ctx)
            { 
                var result = _child.Evaluate(ctx);
                if (_unaryOp == null) return result;
                if (result < ushort.MaxValue)
                {
                    var tmp = (ushort)result;
                    result = _unaryOp switch
                    {
                        "v" or "V" => Lib.UnaryOr16(tmp),
                        "&" => Lib.UnaryAnd16(tmp),
                        "?" => Lib.UnaryXor16(tmp),
                        _ => result
                    };
                }
                else
                {
                    result = _unaryOp switch
                    {
                        "v" or "V" => Lib.UnaryOr32(result),
                        "&" => Lib.UnaryAnd32(result),
                        "?" => Lib.UnaryXor32(result),
                        _ => result
                    };
                }
                return result;
            }
        }
    }
}