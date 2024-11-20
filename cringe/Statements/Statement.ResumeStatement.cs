using INTERCAL.Compiler;
using intercal.Compiler.Lexer;
using INTERCAL.Expressions;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class ResumeStatement : Statement
        {
            private readonly Expression _depth;

            public ResumeStatement(Scanner s)
            {
                s.MoveNext(); 
                if(s.PeekNext.Groups["prefix"].Success || s.PeekNext.Groups["label"].Success)
                    return;
				
                _depth = Expression.CreateExpression(s);
            }

            public override void Emit(CompilationContext ctx)
            {
                // RESUME 0 needs to be treated as a no-op.
                ctx.EmitRaw("   {\r\n");
                ctx.EmitRaw("      uint depth = ");
                _depth.Emit(ctx);
                ctx.EmitRaw(";\r\n");

                if (ctx.DebugBuild)
                {
                    ctx.Emit("      Trace.WriteLine(string.Format(\"      Resuming {0}\", depth));");
                }
                
                ctx.EmitRaw("      if(depth > 0)\r\n");
                ctx.EmitRaw("      {\r\n");
                ctx.EmitRaw("         frame.ExecutionContext.Resume(depth);\r\n");
                ctx.EmitRaw("         goto exit;\r\n");
                ctx.EmitRaw("      }\r\n");
                ctx.EmitRaw("   }\r\n");
            }
        }
    }
}