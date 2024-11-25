using System.Collections.Generic;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Runtime;
using INTERCAL.Statements;

namespace INTERCAL.Expressions
{
    public abstract partial class Expression
    {
        internal class ConstantExpression : Expression
        {
            public readonly uint Value;
            protected static readonly Dictionary<string, EvalDelegate> EvalTable = new Dictionary<string, EvalDelegate>();
			
            protected delegate ushort EvalDelegate(ushort val);

            static ConstantExpression()
            {
                EvalTable["&"] = Lib.UnaryAnd16;
                EvalTable["V"] = Lib.UnaryOr16;
                EvalTable["v"] = Lib.UnaryOr16;
                EvalTable["?"] = Lib.UnaryXor16;
            }

            /// <remarks>
            /// This constructor is used during optimization
            /// </remarks>
            public ConstantExpression(uint val)
            {
                Value = val;
            }

            public ConstantExpression(Scanner s)
            {
                s.MoveNext();
                if (s.Current.Groups["unary_op"].Success)
                {
                    // For some reason there's an unary operator in front of the digits.
                    // We just do the conversion here...
                    var op = s.Current.Value;
                    s.MoveNext();
                    Value = EvalTable[op](ushort.Parse(Statement.ReadGroupValue(s, "digits")));
                }
                else
                {
                    Value = uint.Parse(Statement.ReadGroupValue(s, "digits"));
                }

                // Constant expressions are only 16 bits
                if (Value > ushort.MaxValue)
                    //throw new ParseException(String.Format("line {0}: Constant too big (#{0})", s.LineNumber, Value));
                    throw new CompilationException(Messages.E017);
				
                ReturnType = typeof(ushort);
            }

            public override uint Evaluate(ExecutionContext ctx) => Value; 
			
            public override void Emit(CompilationContext ctx) => ctx.EmitRaw(Value.ToString());

            public override string ToString() => Value.ToString();
        }
    }
}