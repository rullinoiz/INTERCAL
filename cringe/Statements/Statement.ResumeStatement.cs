using System;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Expressions;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class ResumeStatement : Statement
        {
            public const string Token = "RESUME";
            public const string GerundName = "RESUMING";
            
            private readonly Expression _depth;

            public ResumeStatement(Scanner s)
            {
                s.MoveNext(); 
                if (s.PeekNext.Groups["prefix"].Success || s.PeekNext.Groups["label"].Success)
                    return;
				
                _depth = Expression.CreateExpression(s);
                
                // RESUME #0 is an error
                if (_depth is Expression.ConstantExpression { Value: 0 })
                {
                    Console.WriteLine(CompilationWarning.W622);
                }
            }

            public override void Emit(CompilationContext ctx)
            {
                if (ctx.DebugBuild)
                    ctx.Emit("Trace.WriteLine(string.Format(\"\\tResuming {0}\", depth));");
                
                if (_depth is Expression.ConstantExpression { Value: > 0 } d)
                {
                    ctx.Emit($"await {Constants.RuntimeResume}({d.Value});");
                    ctx.Emit($"throw new TaskCanceledException();");
                    return;
                }
                
                // RESUME 0 needs to be treated as a no-op.
                ctx.BeginBlock()
                    .EmitRaw(ctx.Indent() + "uint depth = ");
                    _depth.Emit(ctx);
                    ctx.EmitRaw(";\r\n");
                
                ctx.Emit("if (depth > 0)")
                    .BeginBlock()
                        .Emit($"await {Constants.RuntimeResume}(depth);")
                        .Emit($"throw new TaskCanceledException();")
                    .EndBlock()
                .EndBlock();
            }
        }
    }
}