using INTERCAL.Compiler;
using intercal.Compiler.Lexer;
using INTERCAL.Expressions;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class ForgetStatement : Statement
        {
            private readonly Expression _exp;

            public ForgetStatement(Scanner s)
            {
                s.MoveNext();
                _exp = Expression.CreateExpression(s);
            }

            public override void Emit(CompilationContext ctx)
            {
                /*
                CRAP CRAP CRAP. FORGET drops entries from the stack.
                
                If a program says DO FORGET #1 / DO RESUME #1 we should do the same thing as if they said "RESUME #2". 
                I think the easiest way to do this is to hold an "adjustment" variable - every time you say 
                "FORGET <expr>" we add the result of <expr> to the adjuster. Whenever a RESUME is encountered then we 
                take what we would have returned out of RESUME and add the value of the adjuster. So initially all I did 
                was to add 1 to the number of forgets. forget is a uint because all the assignments added to it are also 
                uints and this avoids a ton of casting. Just adding one doesn't work because it's legal to underflow the 
                stack, so depth-forget might evaluate to less than zero. Thus the extra if here which handles NEXT stack 
                underflow.
                */
                if (ctx.DebugBuild)
                {
                    ctx.EmitRaw("Trace.WriteLine(\"       Forgetting ");
                    _exp.Emit(ctx);
                    ctx.EmitRaw("\");\r\n");
                }

                ctx.EmitRaw("frame.ExecutionContext.Forget(");
                _exp.Emit(ctx);
                ctx.EmitRaw(");\r\n");
            }
        }
    }
}