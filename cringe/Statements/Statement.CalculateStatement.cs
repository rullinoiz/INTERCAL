using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Expressions;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class CalculateStatement : Statement
        {
            private readonly LValue _destination;
            private readonly Expression _expression;
            private readonly bool _isArrayRedimension;

            public CalculateStatement(Scanner s) 
            {
                _destination = new LValue(s);

                s.MoveNext();
                AssertToken(s, "<-");
                s.MoveNext();
				
                _expression = Expression.CreateExpression(s);

                //Is this an array redimension expression?
                if (!_destination.IsArray || _destination.Subscripted) return;
                _isArrayRedimension = true;
                _expression = new Expression.ReDimExpression(s,_expression);
            }
			
            public override void Emit(CompilationContext ctx)
            {
                // Basically we expect to see <lvalue> <- <Expression>
                if (!_isArrayRedimension)
                {
                    if (ctx.DebugBuild)
                    {
             
                        ctx.EmitRaw(ctx.Indent() + $"Trace.WriteLine(string.Format(\"\\t\\t{_destination.Name} <- {{0}}\",");
                        _expression.Emit(ctx); 
                        ctx.EmitRaw("));\r\n");
                    }

                    var lval = _destination.Name;
                    if (!_destination.Subscripted)
                    {
                        ctx.EmitRaw(ctx.Indent() + $"{Constants.FrameExecutionContext}[\"{lval}\"] = ");
                    }
                    else
                    {
                        //ctx[lval, destination.Subscripts(ctx)] = expression.Evaluate(ctx);
                        ctx.EmitRaw(ctx.Indent() + $"{Constants.FrameExecutionContext}[\"{_destination.Name}\", ");
                        _destination.EmitSubscripts(ctx);
                        ctx.EmitRaw("] = ");

                        //Debug.Assert(false);
                    }

                    _expression.Emit(ctx);
                    ctx.EmitRaw(";\r\n");
                }
                else
                {
                    ctx.EmitRaw(ctx.Indent() + $"{Constants.RuntimeReDim}(\"" + _destination.Name + "\",");
                    _expression.Emit(ctx);
                    ctx.EmitRaw(");\r\n");
                }
            }
        }
    }
}