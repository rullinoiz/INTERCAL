using System;
using System.Linq;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using intercal.Compiler.Lexer;
using INTERCAL.Runtime;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class NextStatement : Statement
        {
            public readonly string Target;

            public NextStatement(Scanner s) 
            {
                Target = ReadGroupValue(s, "label");
                s.MoveNext();
                VerifyToken(s, "NEXT");
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
                            var m = t.GetMethod(a.MethodName, new[] { typeof(ExecutionContext) } );
								
                            if (m != null)
                            {
                                if (m.IsStatic)
                                {
                                    //ctx.EmitRaw(a.ClassName + "." + a.MethodName + "(frame.ExecutionContext);");
                                    ctx.EmitRaw("{\r\n");
                                    ctx.EmitRaw(
                                        $"   bool shouldTerminate = {a.ClassName}.{a.MethodName}(frame.ExecutionContext);\r\n");
                                }
                                else
                                {
                                    if (!ctx.ExternalReferences.Contains(a.ClassName))
                                        ctx.ExternalReferences.Add(a.ClassName);

                                    ctx.EmitRaw("{\r\n");
                                    ctx.EmitRaw("   bool shouldTerminate = " 
                                                + CompilationContext.GeneratePropertyName(a.ClassName) 
                                                + "." + a.MethodName + "(frame.ExecutionContext);\r\n");
                                }

                                ctx.EmitRaw("   if (shouldTerminate)\r\n");
                                ctx.EmitRaw("   {\r\n");
                                ctx.EmitRaw("       goto exit;\r\n");
                                ctx.EmitRaw("   }\r\n");
                                if (ctx.DebugBuild) {
                                    ctx.EmitRaw("   else\r\n");
                                    ctx.EmitRaw("   {\r\n");
                                    ctx.EmitRaw($"      Trace.WriteLine(\"Resuming execution at {StatementNumber}\");");
                                    ctx.EmitRaw("   }\r\n");
                                }

                                ctx.EmitRaw("}\r\n");
                            }

                            else
                            {
                                //look for the dynamic one.
                                m = t.GetMethod(a.MethodName, new[] { typeof(ExecutionContext) });
                                if (m != null)
                                {
                                    if (m.IsStatic)
                                    {
                                        ctx.EmitRaw("{\r\n");
                                        ctx.EmitRaw(
                                            $"   bool shouldTerminate = {a.ClassName}.{a.MethodName}(frame.ExecutionContext);\r\n");
                                    }
                                    else
                                    {
                                        if (!ctx.ExternalReferences.Contains(a.ClassName))
                                            ctx.ExternalReferences.Add(a.ClassName);

                                        ctx.EmitRaw("{\r\n");
                                        ctx.EmitRaw("   bool shouldTerminate = " + CompilationContext.GeneratePropertyName(a.ClassName) + "." + a.MethodName + "(frame.ExecutionContext);\r\n");
                                    }

                                    ctx.EmitRaw("   if (shouldTerminate)\r\n");
                                    ctx.EmitRaw("   {\r\n");
                                    ctx.EmitRaw("       goto exit;\r\n");
                                    ctx.EmitRaw("   }\r\n");
                                    if (ctx.DebugBuild)
                                    {
                                        ctx.EmitRaw("   else\r\n");
                                        ctx.EmitRaw("   {\r\n");
                                        ctx.EmitRaw(
                                            $"      Trace.WriteLine(\"Resuming execution at {StatementNumber}\");");
                                        ctx.EmitRaw("   }\r\n");
                                    }

                                    ctx.EmitRaw("}\r\n");
                                }
                                else
                                    throw new CompilationException(string.Format(Messages.E2004, a.ClassName, a.MethodName));
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
                    CompilationContext.Warn(Messages.E129 + Target);
                    ctx.EmitRaw($"Lib.Fail(Messages.E129 + \"{Target}\");\n");
                }

				
                CompilationContext.Warn(Messages.E129 + Target);
                ctx.EmitRaw("Lib.Fail(Messages.E129+ \"" + Target + "\");\n");
            }

            public override void Emit(CompilationContext ctx)
            {
                if (!ctx.Program[Target].Any())
                {
                    //the passed label isn't a local label
                    ctx.Emit($"Trace.WriteLine(\"       Doing {Target} Next\");");
                    EmitExternalCall(ctx);
                }

                else
                {
                    var target = ctx.Program[Target].First();

                    ctx.Emit(!string.IsNullOrEmpty(target.Label)
                        ? $"Trace.WriteLine(\"       Doing {target.Label} Next\");"
                        : $"Trace.WriteLine(\"       Doing statement #{target.StatementNumber} Next\");");

                    ctx.EmitRaw("{\r\n");
                    ctx.EmitRaw("   bool shouldTerminate = frame.ExecutionContext.Evaluate(Eval," 
                                + target.Label.Substring(1, target.Label.Length - 2) + ");\r\n");
                    ctx.EmitRaw("   if (shouldTerminate)\r\n");
                    ctx.EmitRaw("   {\r\n");
                    ctx.EmitRaw("       goto exit;\r\n");
                    ctx.EmitRaw("   }\r\n");

                    if (ctx.DebugBuild)
                    {
                        ctx.EmitRaw("   else\r\n");
                        ctx.EmitRaw("   {\r\n");
                        ctx.EmitRaw($"      Trace.WriteLine(\"Resuming execution at {StatementNumber}\");");
                        ctx.EmitRaw("   }\r\n");
                    }

                    ctx.EmitRaw("}\r\n");

                }
            }
        }
    }
}