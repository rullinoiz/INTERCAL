using System.Diagnostics;
using System.Linq;
using INTERCAL.Compiler;
using intercal.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class ReinstateStatement : AbstainStatement
        {
            public ReinstateStatement(Scanner s) : base(s) { }

            public override void Emit(CompilationContext ctx)
            {
                if (Target != null)
                {
                    var t = ctx.Program[Target].First();
                    Debug.Assert(t.Label == Target);
                    Debug.Assert(t.AbstainSlot >= 0);
                    ctx.EmitRaw("abstainMap[" + t.AbstainSlot + "] = true;\n");
                }
                else
                {
                    foreach (var r in from t in Gerunds from r in ctx.Program.Statements 
                             where r.GetType() == CompilationContext.AbstainMap[t] select r)
                    {
                        ctx.EmitRaw("abstainMap[" + r.AbstainSlot + "] = true;\n");
                    }
                }
            }
        }
    }
}