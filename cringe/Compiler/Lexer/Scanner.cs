using System.Linq;
using System.Text.RegularExpressions;
using INTERCAL.Compiler.Exceptions;

namespace INTERCAL.Compiler.Lexer;

/// <summary>
/// This is the worlds lamest input scanner
/// </summary>
public class Scanner
{
    public static int LineNumber = 1;
    private int _newlines;

    private Scanner(Match m)
    {
        Current = m;
        while (Current.Value == "\n")
        {
            _newlines++;
            Current = Current.NextMatch();
        }
        PeekNext = Current.NextMatch();
    }
        
    public Match Current { get; private set; }

    public Match PeekNext { get; private set; }

    public void MoveNext()
    {
        // The only complication here is that we swallow "\n" internally so the expression parsers never see it.
        Current = PeekNext;
        LineNumber += _newlines;
        _newlines = 0;

        PeekNext = PeekNext.NextMatch();

        while (PeekNext.Value == "\n")
        {
            _newlines++;
            PeekNext = PeekNext.NextMatch();
        }
    }

    public void Panic()
    {
        // We basically start dropping tokens until we find either a "DO/PLEASE DO" or a label followed by a DO/PLEASE DO.
        while (true)
        {
            MoveNext();
            if (Current == Match.Empty)
                break;
            if (Current.Groups["prefix"].Success)
                break;
            if (Current.Groups["label"].Success && PeekNext.Groups["prefix"].Success)
                break;
        }
    }

    public static Scanner CreateScanner(string input)
    {
        var tokens = @"(?<label>(\(\d+\)))|(?<digits>(\d+))|" +
                     "(?<prefix>(PLEASE|DO|N'T|NOT|%))|" +
                     $"(?<gerund>({string.Join("|", CompilationContext.AbstainMap.Keys.ToArray())}))|" +
                     "(?<statement>(READ OUT|WRITE IN|COME FROM|ABSTAIN|REINSTATE|NEXT|STASH|RESUME|FORGET|IGNORE|REMEMBER|RETRIEVE|GIVE UP|NEXT|<-|TRY AGAIN))|" +
                     "(?<separator>(\\\"|\\'|\\+|BY|FROM))|<-|" +
                     @"(?<var>(\.|,|;|:|#))|SUB|" +
                     @"(?<unary_op>(\&|v|V|\?))|" +
                     @"(?<binary_op>(\$|~))|" +
                     //"(?<suffix>(ONCE|AGAIN))|" +
                     @"[a-zA-Z]+|\n";

        var r = new Regex(tokens);
        return new Scanner(r.Match(input));
    }
        
    public string ReadGroupValue(string group)
    {
        if (Current.Groups[group].Success)
            return Current.Groups[group].Value;
        throw new ParseException(string.Format("line {0}: '{2}' is not a valid {1}", LineNumber, group, Current.Value));
    }

    public void VerifyToken(string val)
    {
        if (Current.Value != val)
            throw new ParseException($"line {LineNumber}: Expected a {val}");
    }

}