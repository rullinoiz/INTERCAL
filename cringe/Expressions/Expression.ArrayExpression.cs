using System.Collections.Generic;
using INTERCAL.Compiler;
using intercal.Compiler.Lexer;
using INTERCAL.Runtime;

namespace INTERCAL.Expressions
{
    internal abstract partial class Expression
    {
        public class ArrayExpression : Expression
        {
            // marked public to allow a fix for issue 002.
            public string Name { get; }
            private readonly List<Expression> _subscripts = new List<Expression>();
            public int[] Indices { get; private set; }

            // Consider an expression like
            // ;1 SUB '#4$#2'~'#1$#1' #5
            // When parsing the expression '#4#2' we need to keep track of the fact that the second ' is closing a
            // previous one, not opening a new subscript as in
            // ;3 SUB #4 '#3$#1'
            // DO .1 <- ',4SUB#1'$',4SUB#2'
            public ArrayExpression(Scanner s, string delimeter = null) 
            {
                // Array expressions are VAR SUB <subscript>*
                Name = s.Current.Value + s.PeekNext.Value;
                s.MoveNext();

                if (s.PeekNext.Value != "SUB") return;
                s.MoveNext(); //skip SUB
					
                // We can recognize a subscript coming by the presence of [.,;:#]					
                // If we see a ' or a " that matches delimeter then we know that we are part of a larger expression and
                // return
                char[] expressionStarter = {'.', ',', ':', ';', '#', '\'', '"'};

                while (s.PeekNext.Value.IndexOfAny(expressionStarter) != -1 && s.PeekNext.Value != delimeter)
                {
                    s.MoveNext();
                    _subscripts.Add(CreateExpression(s, delimeter));
                }
            }

            public override uint Evaluate(ExecutionContext ctx) 
            {
                // minor optimization - we reuse the same array object every time instead of re-allocating it
                if (Indices == null)
                    Indices = new int[_subscripts.Count];

                for (var i = 0; i < _subscripts.Count; i++)
                {
                    Indices[i] = (int)_subscripts[i].Evaluate(ctx);
                }
				
                return ctx[Name, Indices];
            }

            public override void Emit(CompilationContext ctx)
            {
                ctx.EmitRaw("frame.ExecutionContext[\"" + Name + "\", new int[]{");

                for (var i = 0; i < _subscripts.Count; i++)
                {
                    ctx.EmitRaw("(int)");
                    _subscripts[i].Emit(ctx);

                    if (i < _subscripts.Count - 1)
                    {
                        ctx.EmitRaw(",");
                    }
                }

                ctx.EmitRaw("}]");
            }
        }
    }
}