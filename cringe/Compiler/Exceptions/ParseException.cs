using System;

namespace INTERCAL.Compiler.Exceptions
{
    /// <summary>
    /// When a parsing error is detected a ParseException is thrown.
    /// </summary>
    internal class ParseException : Exception
    {
        public ParseException(string msg = null) : base(msg) { }
    }
}