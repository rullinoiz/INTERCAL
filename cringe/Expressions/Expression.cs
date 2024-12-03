using System;
using INTERCAL.Compiler;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Compiler.Lexer;
using INTERCAL.Runtime;
using INTERCAL.Statements;

namespace INTERCAL.Expressions;

/// <remarks>
/// INTERCAL expressions always evaluate to either an <see cref="short"/> or an <see cref="int"/>. For
/// simplicity these implementations treat everything as an <see cref="int"/> except for some types of expressions
/// like mingle and select.
/// </remarks>
public abstract partial class Expression
{
    /// <summary>
    /// What does this expression return?
    /// </summary>
    /// <remarks>
    /// In classic INTERCAL there are only two possible types - <see cref="Int16"/> and <see cref="Int32"/>. For
    /// expressions this will contain either <c>typeof(Int16)</c>, <c>typeof(Int32)</c>, or <see langword="null"/>
    /// (if the type of the expression varies at runtime).
    /// </remarks>
    private Type ReturnType { get; init; }

    public static Expression CreateExpression(Scanner s)
    {
        // the 'delimeter' argument is only used in some special cases for arrays
        return CreateExpression(s, null).Optimize();
    }
        
    /// <remarks>
    /// This method might throw a <see cref="ParseException"/> for malformed expressions. This will also be caught
    /// by <see cref="Statement.CreateStatement"/> which will result in splatting the current statement.
    /// </remarks>
    /// <exception cref="ParseException"><c>CreateExpression</c> encountered an error while parsing.</exception>
    private static Expression CreateExpression(Scanner s, string delimeter)
    {
        //<expression> ->
        //#digits    #12345
        //#<unary_operator><digits> #v12345 etc.
        //.<unary_operator>?digits	
        //,<unary_operator>?<digits>
        //:<unary_operator>?digits <subscript>
        //'<expression>'
        //"<expression>"
        //<expression><binary_op><expression>

        //<subscript> ->
        //(SUB<expression>)*

        Expression retval = s.Current.Value switch
        {
            "\"" or "'" => new QuotedExpression(s),
            "#" => new ConstantExpression(s),
            "." or ":" => new NumericExpression(s),
            "," or ";" => new ArrayExpression(s, delimeter),
            "@" or "^" => throw new CompilationException(IntercalError.E997),
            _ => throw new ParseException($"line {Scanner.LineNumber}: Invalid expression {s.Current.Value}")
        };

        // After we've read a valid expression if the next char is a binary operator then we read the other
        // expression and cojoin it with this one.   
        if (s.PeekNext.Value != "$" && s.PeekNext.Value != "~") 
            return retval;
        s.MoveNext();
        var op = s.Current.Value;
        s.MoveNext();

        retval = new BinaryExpression(s, op, retval, CreateExpression(s));

        return retval;
    }

    /// <remarks>
    /// After calling <c>Evaluate</c>, callers can look at the return type to decide if they need to trim down to
    /// 16 bits.
    /// </remarks>
    /// <param name="ctx">The current execution context.</param>
    public abstract uint Evaluate(ExecutionContext ctx);

    public abstract void Emit(CompilationContext ctx);
        
    /// <summary>
    /// Optimizes the <see cref="Expression"/>.
    /// </summary>
    /// <example>
    /// <c>#65535$#65535</c> results in a <see cref="BinaryExpression"/> - calling <c>Optimize</c> on it folds the
    /// constants together into a single <see cref="ConstantExpression"/>.
    /// </example>
    /// <returns>An optimized version of the expression.</returns>
    protected virtual Expression Optimize() 
    {
        // by default we do nothing.
        return this; 
    }

    public override string ToString() => "";
}