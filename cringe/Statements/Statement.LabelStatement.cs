using INTERCAL.Compiler;

namespace INTERCAL.Statements;

public abstract partial class Statement
{
    public class LabelStatement : Statement
    {
        public Statement Statement;
            
        /// <summary>
        /// This is used for COME FROM. If this contains something other than -1, then it contains the index of the
        /// COME FROM.
        /// </summary>
        public int Trapdoor = -1;
            
        public uint LabelNumber;
            
        /// <summary>
        /// A statement may begin with a logical line label enclosed in wax-wane pairs <c>( )</c>. A statement may not
        /// have more than one label, although it is possible to omit the label entirely. A line label is any integer
        /// from 1 to 65535, which must be unique within each program. The user is cautioned, however, that many line
        /// labels between 1000 and 1999 are used in the <see cref="syslib">INTERCAL System Library functions</see>. 
        /// </summary>
        public string Label => $"({LabelNumber})";
            
        /// <summary>
        /// AbstainSlot for labels has to be unique. Without this, an abstain to any label will abstain all labels.
        /// </summary>
        private int _abstainSlot = -1;

        public override int GetAbstainSlot() => _abstainSlot;
        public override void SetAbstainSlot(int value) => _abstainSlot = value;

        public override bool GetEnabled() => Statement.BEnabled;

        public override void Emit(CompilationContext ctx)
        {
            Statement.StatementText = StatementText;
            // if (AbstainSlot > -1)
            // {
            //     
            // }
            Statement.Emit(ctx);
        }
    }
}