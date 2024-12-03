using System;
using System.Linq;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Runtime;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class NextStatement : Statement
    {
        public const string Token = "NEXT";
        public const string GerundName = "NEXTING";
            
        public readonly string Target;

        public NextStatement(Scanner s) 
        {
            Target = ReadGroupValue(s, "label");
            s.MoveNext();
            AssertToken(s, "NEXT");
        }

        private void EmitExternalCall(CompilationContext ctx)
        {
            try
            {
                //If we are referencing a label in another assembly then we 
                //need to figure out what assembly that label lives in.  We'll do
                //this with a dumb ol' search.
                foreach (var e in ctx.References)
                {
                    foreach (var a in e.EntryPoints)
                    {
                        if (a.Label != Target) continue;
                        var t = e.Assembly.GetType(a.ClassName);
                        var m = t!.GetMethod(a.MethodName, [typeof(ExecutionContext)]);
								
                        if (m != null)
                        {
                            ctx.BeginBlock();
                            if (m.IsStatic)
                            {
                                //ctx.EmitRaw(a.ClassName + "." + a.MethodName + "(frame.ExecutionContext);");
                                ctx.Emit(
                                    $"bool shouldTerminate = {a.ClassName}.{a.MethodName}({Constants.FrameExecutionContext});");
                            }
                            else
                            {
                                if (!ctx.ExternalReferences.Contains(a.ClassName))
                                    ctx.ExternalReferences.Add(a.ClassName);

                                ctx.Emit(
                                    $"bool shouldTerminate = {CompilationContext.GeneratePropertyName(a.ClassName)}.{a.MethodName}({Constants.FrameExecutionContext});");
                            }

                            ctx.Emit("if (shouldTerminate)")
                                .BeginBlock()
                                .Emit("goto exit;")
                                .EndBlock();
                            if (ctx.DebugBuild) {
                                ctx.Emit("else")
                                    .BeginBlock()
                                    .EmitRaw($"Trace.WriteLine(\"Resuming execution at {StatementNumber}\");")
                                    .EndBlock();
                            }

                            ctx.EndBlock();
                        }
                        else
                        {
                            //look for the dynamic one.
                            m = t.GetMethod(a.MethodName, [typeof(ExecutionContext)]);
                            if (m != null)
                            {
                                ctx.BeginBlock();
                                if (m.IsStatic)
                                    ctx.Emit(
                                        $"bool shouldTerminate = {a.ClassName}.{a.MethodName}({Constants.FrameExecutionContext});");
                                else
                                {
                                    if (!ctx.ExternalReferences.Contains(a.ClassName))
                                        ctx.ExternalReferences.Add(a.ClassName);

                                    ctx.Emit(
                                        $"bool shouldTerminate = {CompilationContext.GeneratePropertyName(a.ClassName)}.{a.MethodName}({Constants.FrameExecutionContext});\r\n");
                                }

                                ctx.Emit("if (shouldTerminate)")
                                    .BeginBlock()
                                    .Emit("goto exit;")
                                    .EndBlock();
                                if (ctx.DebugBuild)
                                {
                                    ctx.Emit("else")
                                        .BeginBlock()
                                        .Emit($"Trace.WriteLine(\"Resuming execution at {StatementNumber}\");")
                                        .EndBlock();
                                }

                                ctx.EndBlock();
                            }
                            else
                                throw new CompilationException(string.Format(IntercalError.E2004, a.ClassName, a.MethodName));
                        }
                        return;
                    }
                }
            }
            catch (CompilationException)
            {
                throw;
            }
            catch (Exception)
            {
                CompilationContext.Warn(IntercalError.E129 + Target);
                ctx.Emit($"{Constants.LibFail}({nameof(IntercalError)}.{nameof(IntercalError.E129)} + \"{Target}\");");
            }
				
            CompilationContext.Warn(IntercalError.E129 + Target);
            ctx.Emit($"{Constants.LibFail}({nameof(IntercalError)}.{nameof(IntercalError.E129)} + \"{Target}\");");
        }

        public override void Emit(CompilationContext ctx)
        {
            if (!ctx.Program[Target].Any())
            {
                //the passed label isn't a local label
                ctx.Emit($"Trace.WriteLine(\"\\tDoing {Target} Next\");");
                EmitExternalCall(ctx);
            }

            else
            {
                var target = ctx.Program[Target].First();
                var label = target as LabelStatement;
                    
                ctx.Emit(label != null
                        ? $"Trace.WriteLine(\"\\tDoing {label.Label} Next\");"
                        : $"Trace.WriteLine(\"\\tDoing statement #{target.StatementNumber} Next\");")
                    
                    .BeginBlock()
                    .Emit($"bool shouldTerminate = {Constants.RuntimeEvaluate}(Eval,{label?.LabelNumber});")
                    .Emit("if (shouldTerminate)")
                    .BeginBlock()
                    .Emit("goto exit;")
                    .EndBlock();

                if (ctx.DebugBuild)
                {
                    ctx.Emit("else")
                        .BeginBlock()
                        .Emit($"Trace.WriteLine(\"Resuming execution at {StatementNumber}\");")
                        .EndBlock();
                }

                ctx.EndBlock();
            }
        }
    }
}