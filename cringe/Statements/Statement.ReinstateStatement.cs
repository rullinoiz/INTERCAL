using System.Diagnostics;
using System.Linq;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        public class ReinstateStatement : AbstainStatement
        {
            public new const string Token = "REINSTATE";
            public new const string GerundName = "REINSTATING";
            
            public ReinstateStatement(Scanner s) : base(s) { }

            public override void Emit(CompilationContext ctx)
            {
                if (Target != null)
                {
                    var t = ctx.Program[Target].First() as LabelStatement;
                    Debug.Assert(t != null);
                    Debug.Assert(t.GetAbstainSlot() >= 0);
                    CommonEmit(ctx, t.GetAbstainSlot());
                }
                else
                {
                    foreach (var r in from t in Gerunds from r in typeof(Statement).GetNestedTypes() 
                             where r == CompilationContext.AbstainMap[t] select r)
                        CommonEmit(ctx, GetStaticAbstainSlot(r));
                }
            }

            private static void CommonEmit(CompilationContext ctx, int slot)
            {
                ctx.Emit($"if ({Constants.AbstainMapName}[{slot}] > 0)")
                .BeginBlock()
                    .Emit($"{Constants.AbstainMapName}[{slot}] -= 1;")
                .EndBlock();
            }
        }
    }
}