using INTERCAL.Compiler;

namespace INTERCAL.Statements
{
    public abstract partial class Statement
    {
        /// <summary>
        /// <c>TRY AGAIN</c> should be at the very end of the file and should contain nothing after it; not even syntax
        /// errors. Otherwise, <see cref="INTERCAL.Runtime.Messages.E993"/> will be thrown at compile time.
        /// </summary>
        /// <remarks>
        /// <c>TRY AGAIN</c> is specific to C-INTERCAL version 0.25.
        /// </remarks>
        /// <seealso href="https://www.catb.org/~esr/intercal/ick.htm#index-TRY-AGAIN">
        /// INTERCAL Manual, Section 7.9: TRY AGAIN
        /// </seealso>
        public class TryAgainStatement : Statement
        {
            public const string Token = "TRY AGAIN";
            public const string GerundName = "TRYING AGAIN";
            
            static TryAgainStatement()
            {
                SetStaticTraditional(typeof(TryAgainStatement), false);
            }
            
            /// <inheritdoc cref="Statement.TryAgainStatement"/>
            /// <remarks>
            /// If this is -1, no <c>TRY AGAIN</c> has been ecountered.
            /// </remarks>
            public static int TryAgainLine = -1;
            
            public override void Emit(CompilationContext ctx) => ctx.Emit($"goto {Constants.ProgramBeginLabel};");
        }
    }
}