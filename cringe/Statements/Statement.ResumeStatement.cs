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
            }

            public override void Emit(CompilationContext ctx)
            {
                if (ctx.DebugBuild)
                    ctx.Emit("Trace.WriteLine(string.Format(\"\\tResuming {0}\", depth));");
                
                if (_depth is Expression.ConstantExpression d && d.Value > 0)
                {
                    ctx.Emit($"{Constants.RuntimeResume}({d.Value});");
                    ctx.Emit($"goto {Constants.ExitLabelName};");
                    return;
                }
                
                // RESUME 0 needs to be treated as a no-op.
                ctx.BeginBlock()
                    .EmitRaw(ctx.Indent() + "uint depth = ");
                    _depth.Emit(ctx);
                    ctx.EmitRaw(";\r\n");
                
                ctx.Emit("if (depth > 0)")
                    .BeginBlock()
                        .Emit($"{Constants.RuntimeResume}(depth);")
                        .Emit($"goto {Constants.ExitLabelName};")
                    .EndBlock()
                .EndBlock();
            }
        }
    }
}