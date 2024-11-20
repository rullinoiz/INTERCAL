using System;

namespace INTERCAL.Compiler.Exceptions
{
    [Serializable]
    public class CompilationException : Exception
    {
        public CompilationException() { }
        public CompilationException(string message) : base(message) { }
        public CompilationException(string message, Exception inner) : base(message, inner) { }
        protected CompilationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}