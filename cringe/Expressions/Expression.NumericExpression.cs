using System.Diagnostics;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Runtime;
using INTERCAL.Statements;

namespace INTERCAL.Expressions
{
    public abstract partial class Expression
    {
        /// <summary>
        /// A numeric expression is an <see cref="LValue"/> with an optional unary operation on the front.
        /// </summary>
        private class NumericExpression : Expression
        {
            private delegate uint LongOp(uint arg);
            private delegate ushort ShortOp(ushort arg);

            private readonly string _unaryOp;
            private readonly string _lval;

            private readonly LongOp _longform;
            private readonly ShortOp _shortform;

            public NumericExpression(Scanner s)
            {
                var typeid = s.Current.Value;
                switch (typeid)
                {
                    case ":":
                        ReturnType = typeof(uint);
                        break;
                    case ".":
                        ReturnType = typeof(ushort);
                        break;
                    default:
                        Debug.Assert(false);
                        break;
                }
				
                if (s.PeekNext.Groups["unary_op"].Success)
                {
                    //Theres an unary operator
                    s.MoveNext();
                    _unaryOp = s.Current.Value;
                    switch (_unaryOp)
                    {
                        case "v":
                        case "V":
                            _shortform = Lib.UnaryOr16;
                            _longform =  Lib.UnaryOr32;
                            break;
                        case "?":
                            _shortform = Lib.UnaryXor16;
                            _longform =  Lib.UnaryXor32;
                            break;
                        case "&":
                            _shortform = Lib.UnaryAnd16;
                            _longform =  Lib.UnaryAnd32;
                            break;
                    }
                }
			
                s.MoveNext();
                _lval = typeid + Statement.ReadGroupValue(s, "digits");
            }

            public override uint Evaluate(ExecutionContext ctx)
            {
                if (_longform == null)
                    return ctx[_lval];
                /*
				switch(unary_op)
				{
					//This code commented out Jan 29, 2003 as being
					//redundant (I believe)
					
					case "v":
					case "V":
						this.shortform = new short_op(Lib.UnaryOr16);
						this.longform =  new long_op(Lib.UnaryOr32);
						break;
					case "?":
						this.shortform = new short_op(Lib.UnaryXor16);
						this.longform =  new long_op(Lib.UnaryXor32);
						break;
					case "&":
						this.shortform = new short_op(Lib.UnaryAnd16);
						this.longform =  new long_op(Lib.UnaryAnd32);
						break;
				}
				*/
                return ReturnType == typeof(ushort) ? _shortform((ushort)ctx[_lval]) : _longform(ctx[_lval]);
            }
			
            public override void Emit(CompilationContext ctx)
            {
                if (_longform == null)
                    ctx.EmitRaw($"{Constants.FrameExecutionContext}[\"{_lval}\"]");
                else
                {
                    string sf, lf;
                    switch (_unaryOp)
                    {
                        case "v":
                        case "V":
                            sf = Constants.LibUnaryOr16;
                            lf =  Constants.LibUnaryOr32;
                            break;
                        case "?":
                            sf = Constants.LibUnaryXor16;
                            lf =  Constants.LibUnaryXor32;
                            break;
                        case "&":
                            sf = Constants.LibUnaryAnd16;
                            lf =  Constants.LibUnaryAnd32;
                            break;
                        default:
                            throw new CompilationException("Bad unary operator");

                    }

                    ctx.EmitRaw(ReturnType == typeof(ushort)
                        ? $"{sf}(((ushort){Constants.FrameExecutionContext}[\"{_lval}\"]))"
                        : $"{lf}(((uint){Constants.FrameExecutionContext}[\"{_lval}\"]))");
                }
            }
        }
    }
}