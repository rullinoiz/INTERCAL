using System.Collections.Generic;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Expressions;
using INTERCAL.Runtime;
using INTERCAL.Statements;

namespace INTERCAL.Compiler
{
	/// <remarks>
	/// An LValue is a lot like an expression, except resolving it results in a string naming a location. So, for
	/// example, <c>:1</c> is an LValue, but so is <c>;1SUB:1</c>. The latter expression might resolve to a different
	/// location every time.
	/// </remarks>
	public class LValue 
	{
		/// <summary>
		/// Most <see cref="LValue"/>s will not have subscripts, but arrays obviously do, so we track them in a list of
		/// expressions.
		/// </summary>
		private readonly List<Expression> _subscripts;

		public LValue(Scanner s)
		{
			// first read the basic lval...
			Name = Statement.ReadGroupValue(s, "var");
			if (s.Current.Value == "#")
				throw new ParseException($"line {Scanner.LineNumber}: Constants cannot be used as lvalues ");
			
			s.MoveNext();
			Name += Statement.ReadGroupValue(s, "digits");
			
			// Now look to see if we have any subscripts.
			if (s.PeekNext.Value != "SUB") return;
			_subscripts = new List<Expression>();
			s.MoveNext(); //skip SUB
					
			// We can recognize a subscript coming by the presence of [.,;:#]					
			char[] expressionStarter = {'.', ',', ':', ';', '#', '\'', '"'};
			while (s.PeekNext.Value.IndexOfAny(expressionStarter) != -1)
			{
				s.MoveNext();
				_subscripts.Add(Expression.CreateExpression(s));
			}
		}

		public bool IsArray => Name[0] == ',' || Name[0] == ';';
		public bool Subscripted => _subscripts != null;
		public string Name { get; }

		public int[] Subscripts(ExecutionContext ctx)
		{
			if (_subscripts == null)
				return null;
			
			var indices = new int[_subscripts.Count];

			for (var i = 0; i < _subscripts.Count; i++)
			{
				indices[i] = (int)_subscripts[i].Evaluate(ctx);
			}

			return indices;
		}

		/// <summary>
		/// Emits an expression that will hold subscripts, e.g. <c>new int[] {2,2}</c>.
		/// </summary>
		/// <param name="ctx">The current compilation context.</param>
		public void EmitSubscripts(CompilationContext ctx)
		{
			ctx.EmitRaw("new int[] {");
			for (var i = 0; i < _subscripts.Count; i++)
			{
				ctx.EmitRaw("(int)");
				_subscripts[i].Emit(ctx);
				if (i < _subscripts.Count -1)
					ctx.EmitRaw(",");
			}
			ctx.EmitRaw("}");
		}
	}
}