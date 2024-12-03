using System.Collections.Generic;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Lexer;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class WriteInStatement : Statement
    {
        public const string Token = "WRITE IN";
        public const string GerundName = "WRITING IN";
            
        private readonly List<LValue> _lvals = [];

        public WriteInStatement(Scanner s)
        {
            s.MoveNext();
            _lvals.Add(new LValue(s));
            while (s.PeekNext.Value == "+")
            {
                s.MoveNext();
                s.MoveNext();
                _lvals.Add(new LValue(s));
            }
        }

        public override void Emit(CompilationContext ctx)
        {
            foreach (var lval in _lvals)
                ctx.Emit($"{Constants.RuntimeWriteIn}(\"{lval.Name}\");");
        }
    }
}