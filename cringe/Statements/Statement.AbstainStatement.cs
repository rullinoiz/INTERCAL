using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using INTERCAL.Compiler;
using INTERCAL.Runtime;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Expressions;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class AbstainStatement : Statement
    {
        public const string Token = "ABSTAIN";
        public const string GerundName = "ABSTAINING";
            
        // One or the other of these will be non-null
        public readonly List<string> Gerunds;
        public readonly string Target;
        private readonly Expression _times;
            
        /// <summary>
        /// <c>ABSTAIN</c> contains extra functionality specific to C-INTERCAL (since version 0.25) called a
        /// computed abstain. If we ever enable that functionality, <c>ABSTAIN</c> is no longer traditional.
        /// </summary>
        public AbstainStatement(Scanner s)
        {
            s.MoveNext();
                
            // this shouldn't be run in ReinstateStatement
            if (GetType() == typeof(AbstainStatement))
            {
                if (s.Current.Value != "FROM")
                {
                    _times = Expression.CreateExpression(s);
                    Traditional = false;
                    s.MoveNext();
                }

                s.MoveNext();
            }

            if (s.Current.Groups["gerund"].Success)
            {
                Gerunds = [s.Current.Value];
							
                while (s.PeekNext.Value == "+")
                {
                    s.MoveNext();
                    s.MoveNext();
                    Gerunds.Add(ReadGroupValue(s,"gerund"));
                }
            }
            else if (s.Current.Groups["label"].Success)
                Target = s.Current.Value;
            else 
                throw new ParseException($"line {Scanner.LineNumber}: Invalid statement");
        }

        public override void Emit(CompilationContext ctx)
        {
            if (Target != null)
            {
                if (ctx.Program[Target].FirstOrDefault() is LabelStatement t)
                { 
                    Debug.Assert(t.Label == Target);
                    Debug.Assert(t.GetAbstainSlot() >= 0);
                    CommonEmit(ctx, t.GetAbstainSlot());
                }
                else
                    ctx.Emit($"{Constants.LibFail}(\"{IntercalError.E139}{Target}\");");
            }
            else
            {
                foreach (var r in from t in Gerunds from r in typeof(Statement).GetNestedTypes() 
                         where r == CompilationContext.AbstainMap[t] 
                         select r)
                    CommonEmit(ctx, GetStaticAbstainSlot(r));
            }
        }

        private void CommonEmit(CompilationContext ctx, int slot)
        {
            // ABSTAIN FROM should only change the abstainMap if it is currently zero
            if (_times == null)
            {
                ctx.Emit($"if ({Constants.AbstainMapName}[{slot}] == 0)")
                    .BeginBlock()
                    .Emit($"{Constants.AbstainMapName}[{slot}] = 1;")
                    .EndBlock();
                return;
            }
                
            // ABSTAIN <expr> FROM can add to the abstainMap whenever
            ctx.EmitRaw(ctx.Indent() + $"{Constants.AbstainMapName}[{slot}] += ");
            _times.Emit(ctx);
            ctx.EmitRaw(";\r\n");
        }
    }
}