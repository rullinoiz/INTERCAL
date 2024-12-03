using System;

namespace INTERCAL.Compiler.Exceptions;

/// <summary>
/// When a parsing error is detected a ParseException is thrown.
/// </summary>
internal class ParseException(string msg = null) : Exception(msg);