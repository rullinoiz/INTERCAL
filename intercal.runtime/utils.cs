using System;
using System.IO;
using System.Text;
using System.Diagnostics;
// using System.Runtime.Remoting.Messaging;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace INTERCAL.Runtime;

/// <remarks>
/// These error message texts, with one exception, are direct from the Princeton compiler (INTERCAL-72)
/// sources (transmitted by Don Woods).
/// The one exception is <see cref="E632">E632</see>, which in INTERCAL-72 had the error message
/// <code>
/// PROGRAM ATTEMPTED TO EXIT WITHOUT ERROR MESSAGE
///     ESR's "THE NEXT STACK HAS RUPTURED!..."
/// </code>
/// has been retained on the grounds that it is more obscure and much funnier. For context, find a copy of
/// Joe Haldeman's SF short story "A !Tangled Web", first published in Analog magazine sometime in 1983 and
/// later anthologized in the author's "Infinite Dreams" (Ace 1985).
/// </remarks>
public static class IntercalError
{
    #region Standard Error Messages
        
    /// <summary>
    /// An undecodable statement has been encountered in the course of execution.
    /// </summary>
    public const string E000 = "E000 %s";
        
    /// <summary>
    /// An expression contains a syntax error.
    /// </summary>
    public const string E017 = "E017 DO YOU EXPECT ME TO FIGURE THIS OUT?\n   ON THE WAY TO {0}";
        
    /// <summary>
    /// Improper use has been made of statement identifiers.
    /// </summary>
    public const string E079 = "E079 PROGRAMMER IS INSUFFICIENTLY POLITE";
        
    /// <summary>
    /// Improper use has been made of statement identifiers.
    /// </summary>
    public const string E099 = "E099 PROGRAMMER IS OVERLY POLITE";
        
    /// <summary>
    /// Program has attempted 80 levels of NEXTing.
    /// </summary>
    public const string E123 = "E123 PROGRAM HAS DISAPPEARED INTO THE BLACK LAGOON";
        
    /// <summary>
    /// Program has attempted to transfer to a non-existent line label.
    /// </summary>
    public const string E129 = "E129 PROGRAM HAS GOTTEN LOST ON THE WAY TO ";
        
    /* DONE */
    /// <summary>
    /// An ABSTAIN or REINSTATE statement references a non-existent line label.
    /// </summary>
    public const string E139 = "E139 I WASN'T PLANNING TO GO THERE ANYWAY";
        
    /* DONE */
    /// <summary>
    /// A line label has been defined more than once.
    /// </summary>
    /// <example>
    /// <code>
    /// (150)   DO NOTHING
    ///         ...
    /// (150)   PLEASE DO .1 &lt;- #65535
    /// </code>
    /// </example>
    public const string E182 = "E182 YOU MUST LIKE THIS LABEL A LOT!";
        
    /* DONE */
    /// <summary>
    /// An invalid line label has been encountered.
    /// </summary>
    public const string E197 = "E197 SO! 65535 LABELS AREN'T ENOUGH FOR YOU?";
        
    /// <summary>
    /// An expression involves an unidentified variable.
    /// </summary>
    /// <example>
    /// <code>
    /// NOTE THAT .2 IS NOT DEFINED
    /// DO .1 &lt;- .2
    /// </code>
    /// </example>
    public const string E200 = "E200 NOTHING VENTURED, NOTHING GAINED";
        
    /// <summary>
    /// An attempt has been made to give an array a dimension of zero.
    /// </summary>
    /// <example>
    /// <code>
    /// DO ,1 &lt;- #0
    /// </code>
    /// </example>
    public const string E240 = "E240 ERROR HANDLER PRINTED SNIDE REMARK";
        
    /* DONE */
    /// <summary>
    /// Invalid dimensioning information was supplied in defining or using an array. Usually occurs when trying
    /// to assign an element that is out of bounds.
    /// </summary>
    /// <example>
    /// <code>
    /// PLEASE NOTE THAT ,1 IS A 16 BIT ARRAY WITH TWO ELEMENTS
    /// DO ,1 &lt;- #2
    /// NOTE THAT ARRAYS START AT 1
    /// DO ,1 SUB #0 &lt;- #65535
    /// </code>
    /// </example>
    public const string E241 = "E241 VARIABLES MAY NOT BE STORED IN WEST HYPERSPACE";
        
    /* DONE */
    /// <summary>
    /// A 32-bit value has been assigned to a 16-bit variable.
    /// </summary>
    public const string E275 = "E275 DON'T BYTE OFF MORE THAN YOU CAN CHEW";
        
    /* DONE */
    /// <summary>
    /// A retrieval has been attempted for an unSTASHed value.
    /// </summary>
    public const string E436 = "E436 THROW STICK BEFORE RETRIEVING!";
        
    /* DONE */
    /// <summary>
    /// A <c>WRITE IN</c> statement or interleave <c>$</c> operation has produced value requiring over 32 bits
    /// to represent.
    /// </summary>
    public const string E533 = "E533 YOU WANT MAYBE WE SHOULD IMPLEMENT 64-BIT VARIABLES?";
        
    /// <summary>
    /// Insufficient data. (raised by reading past EOF)
    /// </summary>
    public const string E562 = "E562 I DO NOT COMPUTE";
        
    /// <summary>
    /// Input data is invalid.
    /// </summary>
    public const string E579 = "E579 WHAT BASE AND/OR LANGUAGE INCLUDES \"{0}\" ???";
        
    /* DONE */
    /// <summary>
    /// The expression of a <c>RESUME</c> statement evaluated to <c>#0</c>.
    /// </summary>
    public const string E621 = "E621 ERROR TYPE 621 ENCOUNTERED";
        
    /* NOT DONE */
    /// <summary>
    /// Program execution terminated via a <c>RESUME</c> statement instead of <c>GIVE UP</c>.
    /// </summary>
    public const string E632 = "E632 THE NEXT STACK RUPTURES. ALL DIE. OH, THE EMBARRASSMENT!";
        
    /* DONE */
    /// <summary>
    /// Execution has passed beyond the last statement of the program.
    /// </summary>
    public const string E633 = "E633 PROGRAM FELL OFF THE EDGE ON THE WAY TO THE NEW WORLD\n";
        
    /* DONE */
    /// <summary>
    /// A compiler error has occurred.
    /// </summary>
    public const string E774 = "E774 RANDOM COMPILER BUG";
        
    /// <summary>
    /// An unexplainable compiler error has occurred.
    /// </summary>
    public const string E778 = "E778 UNEXPLAINED COMPILER BUG";
        
    #endregion
        
    #region INTERCAL.NEXT and C-INTERCAL
        
    /// <summary>
    /// You tried to use a non-standard INTERCAL-72 statement with the 'traditional' flag on.
    /// </summary>
    public const string E111 = "E111 COMMUNIST PLOT DETECTED, COMPILER IS SUICIDING";
        
    /// <summary>
    /// Cannot find the magically included system library.
    /// </summary>
    public const string E127 = "E127 SAYING 'ABRACADABRA' WITHOUT A MAGIC WAND WON'T DO YOU ANY GOOD\n\tON THE WAY TO THE CLOSET";
        
    /// <summary>
    /// Out of stash space.
    /// </summary>
    public const string E222 = "E222 BUMMER, DUDE!";
        
    /// <summary>
    /// Too many variables.
    /// </summary>
    public const string E333 = "E333 YOU CAN'T HAVE EVERYTHING, WHERE WOULD YOU PUT IT?";
        
    /// <summary>
    /// A COME FROM statement references a non-existent line label.
    /// </summary>
    public const string E444 = "E444 IT CAME FROM BEYOND SPACE";
        
    /// <summary>
    /// More than one <c>COME FROM</c> statement references the same label.
    /// </summary>
    /// <example>
    /// <code>
    /// (150)   PLEASE DO NOTHING
    ///         ...
    ///         DO COME FROM (150)
    ///         ...
    ///         DO COME FROM (150)
    /// </code>
    /// </example>
    public const string E555 = "E555 FLOW DIAGRAM IS EXCESSIVELY CONNECTED ";
        
    /// <summary>
    /// Too many source lines.
    /// </summary>
    public const string E666 = "E666 COMPILER HAS INDIGESTION";
        
    /* DONE */
    /// <summary>
    /// No such source file.
    /// </summary>
    public const string E777 = "E777 A SOURCE IS A SOURCE, OF COURSE, OF COURSE";
        
    /// <summary>
    /// Can't open C output file.
    /// </summary>
    public const string E888 = "E888 I HAVE NO FILE AND I MUST SCREAM";

    /// <summary>
    /// <c>TRY AGAIN</c>, if used, is not at the very end of the file.
    /// </summary>
    public const string E993 = "E993 I GAVE UP LONG AGO";
        
    /// <summary>
    /// An expression uses an operator that only makes sense in TriINTERCAL, which is base 3 and above.
    /// </summary>
    public const string E997 = "E997 ILLEGAL POSSESSION OF A CONTROLLED UNARY OPERATOR";
        
    /* DONE */
    /// <summary>
    /// Source file named with an invalid extension.
    /// </summary>
    public const string E998 = "E998 EXCUSE ME, YOU MUST HAVE ME CONFUSED WITH SOME OTHER COMPILER";
        
    /// <summary>
    /// Can't open C skeleton file.
    /// </summary>
    public const string E999 = "E999 NO SKELETON IN MY CLOSET, WOE IS ME!";
        
    #endregion

    #region SICK Errors

    /* DONE */
    /// <summary>
    /// User specified <c>/t:</c> with something other than <c>exe</c> or <c>library</c>.
    /// </summary>
    public const string E2001 = "E2001 DON'T GET MUCH CALL FOR THOSE 'ROUND THESE PARTS";
        
    /* DONE */
    /// <summary>
    /// Unable to open as assembly passed with <c>/r</c> or unable to load assembly at runtime.
    /// </summary>
    public const string E2002 = "E2002 SOME ASSEMBLY REQUIRED";
        
    /* DONE */
    /// <summary>
    /// Something went wrong when shelling out to <c>csc</c>. (csc.exe is probably not on the PATH)
    /// </summary>
    public const string E2003 = "E2003 C-SHARP OR B-FLAT";
        
    /// <summary>
    /// An extension function referenced with <c>/r</c> had the wrong prototype.
    /// </summary>
    public const string E2004 = "E2004 SQUARE PEG, ROUND HOLE\n\tON THE WAY TO {0}.{1}";
        
    #endregion
}
    
/// <summary>
/// Intercal libraries use this assembly attribute to route calls to functions.
/// </summary>
/// <example>
/// <code>[assembly: EntryPoint("(3000)", "Class", "method")]</code>
/// In this case the function <c>Class.method</c> will be called whenever a module containing <c>DO (3000) NEXT</c> links 
/// to the library in question.
/// </example>
/// <remarks>
/// <c>Class.method</c> can be static or instance and can take one of two forms:
/// <code>public void foobar(ExecutionContext ctx)</code>
/// or:
/// <code>public void Method(ExecutionContext ctx, string Label)</code>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class EntryPointAttribute(string label, string className, string methodName) : Attribute
{
    /// <summary>
    /// An INTERCAL label to link to.
    /// </summary>
    /// <remarks>
    /// May not be wildcarded (too much room for ambiguity)
    /// </remarks>
    public readonly string Label = label;
        
    /// <summary>
    /// Name of the class which contains the <see cref="MethodName"/>.
    /// </summary>
    public readonly string ClassName = className;
        
    /// <summary>
    /// Name of the method to link.
    /// </summary>
    /// <remarks>
    /// This method must be of type IntercalExtensionDelegate.
    /// </remarks>
    public readonly string MethodName = methodName;
}

[Serializable]
public class IntercalException : Exception
{
    public IntercalException() : base("") { }
    public IntercalException(string message) : base(message) { }
    public IntercalException(string message, Exception inner) : base(message, inner) { }
    [Obsolete("Obsolete")]
    protected IntercalException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
    
/// <summary>
/// IExecutionContext holds shared variables used to call across components. INTERCAL uses an interface so that
/// other languages can define their own implementation of this interface and pass it in to the <c>DO</c> functions.
/// This allows them to hook variable manipulation and implement a variable store however they like.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Accessor for non-array variables.
    /// </summary>
    /// <param name="varname">A non-array INTERCAL variable.</param>
    uint this[string varname] { get; set; }
        
    /// <summary>
    /// Accessor for array variables.
    /// </summary>
    /// <param name="varname">An array INTERCAL variable.</param>
    /// <param name="indices"></param>
    uint this[string varname, int[] indices] { get; set; }
        
    /// <summary>
    /// Tracks the input and output tape positions per the Turing Text model.
    /// </summary>
    uint LastIn { get; }
        
    /// <inheritdoc cref="LastIn"/>
    uint LastOut { get; }
    void AdvanceInput(uint delta);
    void AdvanceOutput(uint delta);
    void ReadOut(string s);
    void WriteIn(string s);
        
    // These are mostly helper functions. IsArray should be moved to
    // INTERCAL.Runtime.Lib, but the rest basically just implement stash/retrieve
    // and ignore/remember.
    void ReDim(string var, int[] dimensions);
    void Stash(params string[] vars);
    void Retrieve(params string[] vars);
    void Ignore(params string[] labels);
    void Remember(params string[] labels);
}
    
[Serializable]
public class ExecutionContext : AsyncDispatcher, IExecutionContext
{
    #region Fields and constuctors

    public static ExecutionContext CreateExecutionContext() => new();

    private ExecutionContext()
    {
        Input = Console.OpenStandardInput();
        Output = Console.OpenStandardOutput();
        TextOut = new StreamWriter(Output);
        BinaryOut = new BinaryReader(Input);
        TextIn = new StreamReader(Input);
    }
        
    /// <summary>
    /// Text I/O is done in INTERCAL by attaching streams.
    /// By default input and output come from the console but programs are free to change that if they wish.
    /// </summary>
    public Stream Input { get; set; }
        
    /// <inheritdoc cref="Input"/>
    public Stream Output { get; private set; }

    public TextReader TextIn { get; private set; } 
        
    public TextWriter TextOut { get; private set; }
        
    public BinaryReader BinaryOut { get; private set; }

    /// <summary>
    /// The Turing text model is not very component friendly because whatever you write out is dependent on
    /// what the *last* guy did. In order for components to be able to share strings (to do string manipulation)
    /// LastIn and LastOut MUST be stored in the execution context. Furthermore - there has to be some way to query it.
    /// </summary>
    public uint LastIn { get; private set; }
    /// <inheritdoc cref="LastIn"/>
    public uint LastOut { get; private set; }

    public void AdvanceInput(uint delta) => LastIn += delta % 255; 
    public void AdvanceOutput(uint delta) => LastOut += delta % 255;

    public uint this[string varname]
    {
        get
        {
            if (!_variables.ContainsKey(varname))
                Lib.Fail(IntercalError.E200 + " (" + varname + ")");

            var v = GetVariable(varname);

            if (v is IntVariable variable)
                return variable.Value; 
                
            Lib.Fail(IntercalError.E241);
            // This will never execute - Fail() always throws an exception
            return 0;
        }
        set
        {
            var v = GetVariable(varname);

            if (v is IntVariable variable)
            {
                if (variable.Name[0] == '.' && value > ushort.MaxValue)
                    Lib.Fail(IntercalError.E275);

                variable.Value = value;
            }
            else
                Lib.Fail(IntercalError.E241);
        }
    }

    public uint this[string varname, int[] indices]
    {
        get
        {
            var v = GetVariable(varname);
            if (v is ArrayVariable variable)
            {
                return variable[indices];
            }
            Lib.Fail(IntercalError.E241);

            //This will never execute - Fail() always throws an exception
            return 0;
        }
        set
        {
            var v = GetVariable(varname);
            if (v is ArrayVariable variable)
            {
                variable[indices] = value;
            }
            else
                Lib.Fail(IntercalError.E241);
        }
    }

    private interface IVariable
    {
        string Name { get; set; }
        bool Enabled { get; set; }
        void Stash();
        void Retrieve();
    }
        
    /// <summary>
    /// Variables are always shared across components, just like they were in the traditional public library.
    /// </summary>
    [Serializable]
    private abstract class Variable<T>(ExecutionContext ctx, string name) : IVariable
    {
        protected ExecutionContext Owner = ctx;
        public string Name { get; set; } = name;
        public bool Enabled { get; set; } = true;
            
        /// <summary>
        /// Each variable has it's own little stack for stashing/retrieving values...
        /// </summary>
        protected Stack<T> StashStack = new();

        public abstract void Stash();
        public abstract void Retrieve();
    }
        
    /// <inheritdoc cref="Variable{T}"/>
    /// <remarks>
    /// Spot (.) and Two-spot (:) variables are both stored as <see cref="IntVariable"/>s.
    /// </remarks>
    [Serializable]
    private class IntVariable(ExecutionContext ctx, string name) : Variable<uint>(ctx, name), IVariable
    {
        // ReSharper disable once UnusedMember.Local
        private static Random _random = new();
        public uint Val;

        public uint Value
        {
            get => Val;
            set { if (Enabled) Val = value; }
        }

        public override void Stash() => StashStack.Push(Val);

        public override void Retrieve()
        {
            Trace.Write($"\t{name} contains: ");
            foreach (var item in StashStack)
            {
                Trace.Write(item + " ");
            }
            Trace.WriteLine(null);
            try
            {
                Val = StashStack.Pop();
            }
            catch
            {
                Lib.Fail(IntercalError.E436);
            }
        }
            
        public override string ToString() => Value.ToString();
    }
        
    /// <inheritdoc cref="Variable{T}" />
    [Serializable]
    private class ArrayVariable(ExecutionContext ctx, string name) : Variable<Array>(ctx, name), IVariable
    {
        private Array _values;

        public void ReDim(int[] subscripts)
        {
            foreach (var i in subscripts)
            {
                if (i == 0) 
                    Lib.Fail(IntercalError.E240);
            }
                
            var lbounds = new int[subscripts.Length];

            for (var i = 0; i < lbounds.Length; i++)
            {
                lbounds[i] = 1;
            }
            _values = Array.CreateInstance(typeof(uint), subscripts, lbounds);
            _values.SetValue(new uint(), subscripts);
        }
            
        public uint this[int[] indices]
        {
            get
            {
                try
                {
                    return (uint)_values.GetValue(indices)!;
                }
                catch
                {
                    Lib.Fail(IntercalError.E241);
                    return 0;
                }
            }
            set
            {
                try
                {
                    _values.SetValue(value, indices);
                }
                catch (Exception e)
                {
                    Console.Write($"{e.Message} var=\"{Name}\" val=\"{value}\" indices={{");
                    foreach (var i in indices)
                        Console.Write(i);
                    Console.WriteLine("}");
                    Lib.Fail(IntercalError.E241);
                }
            }
        }

        public int Rank => _values.Rank;
            
        public int GetLowerBound(int dim) { return _values.GetLowerBound(dim); }
            
        public int GetUpperBound(int dim) { return _values.GetUpperBound(dim); }
            
        public override void Stash()
        {
            //what to do if a program stashes an unitialized array?  Donald Knuth's
            //tpk.i depends on this not crashing the runtime.  Knuth is more important
            //than you or I so this runtime bends to his wishes. This does mean that
            //it is possible to RETRIEVE a null array.
            if (_values != null)
                StashStack.Push(_values.Clone() as Array);
            else
                StashStack.Push(null);
        }

        public override void Retrieve()
        {
            if (StashStack.Count > 0)
                _values = StashStack.Pop();
            else
                Lib.Fail(IntercalError.E436);
        }
            
        public override string ToString()
        {
            var sb = new StringBuilder();
            // var idx = new int[1];

            foreach (uint v in _values)
            {
                var c = Owner.LastOut - v;

                Owner.LastOut = c;

                c = (c & 0x0f) << 4 | (c & 0xf0) >> 4;
                c = (c & 0x33) << 2 | (c & 0xcc) >> 2;
                c = (c & 0x55) << 1 | (c & 0xaa) >> 1;

                sb.Append((char)c);
            }

            return sb.ToString();
        }
    }

    //This dictionary maps simple identifiers to their values.  All non-array values are 
    //stored here.  Entries in arrays are stored in the Arrays hash table below.
    private readonly Dictionary<string, IVariable> _variables = new();

    #endregion

    #region control flow
    public async Task Run(IntercalThreadProc proc)
    {
        // StartProc sp = Evaluate;

        await Evaluate(proc, 0);
        Trace.WriteLine("ENTRY THREAD FINISHED");
            
        if (CurrentException != null)
        {
            throw CurrentException;
        }
    }
        
    public async Task Evaluate(IntercalThreadProc proc, int label)
    {
        Trace.WriteLine("\tEvaluating " + label);
        var frame = new ExecutionFrame(this, proc, label);

        await using (await SyncLock.LockAsync(CancellationToken.None))
        {
            Trace.WriteLine("\tLocking in " + label);
            if (label > 0)
            {
                if (NextingStack.Count >= 80)
                    Lib.Fail(IntercalError.E123);
                NextingStack.Push(frame); 
            }
            
            Trace.WriteLine("\tAwaiting " + label);
            await (frame.RunningTask = frame.Start());
            Trace.WriteLine("\tFinished " + label);
        }
        Trace.WriteLine("\tUnlocked " + label);
    }

    #endregion

    #region STASH/RETRIEVE
    /// <remarks>
    /// <c>STASH</c> / <c>RETRIEVE</c> always operate on the global execution context - all variables have visibility to
    /// everyone in the program flow. Note that there is no way to know if any given identifier is currently
    /// holding a value set by another component or is just uninitialized. Such is the power of INTERCAL!
    /// Perhaps every module should track in its metadata a listing of the identifiers used in that component?
    /// These would take the form of assembly attributes.
    /// </remarks>
    /// <param name="varname">INTERCAL variable name.</param>
    /// <returns>An INTERCAL variable.</returns>
    private IVariable GetVariable(string varname)
    {
        IVariable retval = null;

        switch (varname[0])
        {
            case '.':
            case ':':
            {
                if (!_variables.TryGetValue(varname, out retval))
                {
                    IVariable v = new IntVariable(this, varname);
                    _variables[varname] = v;
                    retval = v;
                }
                break;
            }
            case ',':
            case ';':
            {
                if (!_variables.TryGetValue(varname, out retval))
                {
                    IVariable v = new ArrayVariable(this, varname);
                    _variables[varname] = v;
                    retval = v;
                }
                break;
            }
            default:
                Lib.Fail(IntercalError.E241);
                break;
        }

        return retval;
    }
        
    /// <remarks>
    /// Is there any reason we can't just use native array classes? Actually, yes. The execution engine holds
    /// onto the variables because of <see cref="Stash"/> / <see cref="Retrieve"/>. Hmm, is that convincing?
    /// Would there be harm in just giving clients an object reference? (which would support stashing /
    /// retrieving)?
    /// </remarks>
    /// <param name="var">Variable name.</param>
    /// <param name="dimensions">Dimensions of the array.</param>
    public void ReDim(string var, int[] dimensions)
    {
        if (GetVariable(var) is ArrayVariable v)
            v.ReDim(dimensions);
        else
            Lib.Fail(IntercalError.E000);
    }

    public void Stash(params string[] vars)
    {
        foreach (var v in vars) GetVariable(v).Stash();
    }

    public void Retrieve(params string[] vars)
    {
        foreach (var v in vars) GetVariable(v).Retrieve();
    }
    #endregion

    #region IGNORE/REMEMBER
    /// <remarks>
    /// <c>IGNORE</c> / <c>REMEMBER</c> are global because variables are visible everywhere. If module A
    /// <c>IGNORE</c>s a variable and passes it to B any assigns that B makes will be ignored. This means B can
    /// ignore and return back to A and A has no good way to even determine if any given variable is currently
    /// ignored.
    /// </remarks>
    /// <param name="labels">A variable in an INTERCAL program.</param>
    public void Ignore(params string[] labels)
    {
        foreach (var label in labels)
        {
            GetVariable(label).Enabled = false;
        }
    }

    /// <inheritdoc cref="Ignore"/>
    public void Remember(params string[] labels)
    {
        foreach (var label in labels)
        {
            GetVariable(label).Enabled = true;
        }
    }

    #endregion

    #region READ/WRITE
    /// <summary>
    /// The execution context exposes two public properties (an input stream and an output stream). Programs
    /// hosting INTERCAL components can do string communication by hooking the output stream and calling
    /// routines that do a <c>DO READ OUT</c>.
    /// </summary>
    /// <remarks>
    /// String manipulation is impossible. Suppose an INTERCAL module calls a C# module, and the C# module wants
    /// to do string manipulation on the string stored in <c>;0</c>. In order to decipher the characters in the
    /// array it will be necessary for the C# module to where the input tape was positioned when the characters
    /// were read in (since strings are stored as deltas rather than absolute values).  For example, if the
    /// array contains <c>{65, 1, 1, 1}</c> and <see cref="LastIn"/> is 68 then you could ostensibly conclude
    /// that the string contains <c>{A B C D}</c>, but this is only true if the array was the last one written
    /// to. In keeping with the spirit of the Turing Text model I think the context should save the current
    /// input tape position whenever a <c>WRITE IN</c> is encountered, e.g. <c>(0) {65,1,1,1}</c> is enough
    /// information to recover <c>"ABCD"</c>. Existing programs continue to work; new components can peek at the
    /// value if they want to do string manipulation. Hopefully we can make this completely transparent to
    /// modules written in INTERCAL. As of right now I haven't done anything yet to enable this.
    /// </remarks>
    public void ReadOut(string identifier)
    {
        Trace.WriteLine($"Reading out variable '{identifier.Length}'");

        var next = _variables[identifier].ToString();
        Trace.WriteLine($"Reading out value '{next}'");

        if (_variables[identifier] is ArrayVariable)
            TextOut.Write(next);
        else
            TextOut.WriteLine(next);

        TextOut.Flush();
    }
        
    /// <inheritdoc cref="ReadOut(string)"/>
    public void ReadOut(object expression)
    {
        Trace.WriteLine($"Reading out object '{expression}'");
        TextOut.WriteLine(expression);
        TextOut.Flush();
    }

    public void WriteIn(string identifier)
    {
        Trace.WriteLine($"Writing into {identifier}");
        //the intercal model is stream-based - calling WriteIn reads as
        //many chars as there are in the array (or fewer if EOF is reached)
        //Console.Write("{0}?>", s);

        var idx = new int[1];

        if (identifier[0] == ',' || identifier[0] == ';')
        {
            if (_variables[identifier] is not ArrayVariable av) 
                throw new IntercalException(IntercalError.E200);
                
            if (av.Rank != 1) Lib.Fail(IntercalError.E241);

            for (var i = av.GetLowerBound(0); i <= av.GetUpperBound(0); i++)
            {
                idx[0] = i;

                var c = (uint)BinaryOut.ReadChar();

                var v = (c - LastIn) % 256;
                LastIn = c;

                Trace.WriteLine($"Writing '{(char)c}' into index {i}");
                this[identifier, idx] = v;
            }
        }
        else
        {
            var input = TextIn.ReadLine();
            if (input == null)
            {
                Lib.Fail(string.Format(IntercalError.E579, (string)null));
                return; // Lib.Fail throws an exception
            }
            try
            {
                // Note that this compiler today only works in wimpmode.  To do it right we will need to have
                // satellite assemblies, one for each of many different languages.
                this[identifier] = uint.Parse(input);
            }
            catch
            {
                Lib.Fail(string.Format(IntercalError.E579, input));
            } 
        }
    }

    #endregion
}
    
/// <summary>
/// This class provides basic bit-mangling functionality
/// </summary>
/// <example>
/// <code>
/// uint u = Lib.Mingle(0, 65535);
/// </code>
/// </example>
public static class Lib
{
    private static readonly Random Random = new();

    private static readonly uint[] Bitflags =
    [
        0x00000001, 0x00000002, 0x00000004, 0x00000008,
        0x00000010, 0x00000020, 0x00000040, 0x00000080,
        0x00000100, 0x00000200, 0x00000400, 0x00000800,
        0x00001000, 0x00002000, 0x00004000, 0x00008000,
        0x00010000, 0x00020000, 0x00040000, 0x00080000,
        0x00100000, 0x00200000, 0x00400000, 0x00800000,
        0x01000000, 0x02000000, 0x04000000, 0x08000000,
        0x10000000, 0x20000000, 0x40000000, 0x80000000
    ];
        
    /// <summary>
    /// Takes two 16-bit values and builds a 32-bit operator by "mingling" their bits.
    /// </summary>
    /// <example>
    /// An example taken from Section 3.4.1 of the INTERCAL manual:
    /// <code>
    /// DO :1 &lt;- #65535$#0
    /// DO NOTE THAT :1 IS EQUAL TO 2863311530 IN DECIMAL, OR 101010... IN BINARY
    /// </code>
    /// <code>
    /// DO :1 &lt;- #0$#65535
    /// DO NOTE THAT :1 IS EQUAL TO 1431655765 IN DECIMAL, OR 010101... IN BINARY
    /// </code>
    /// </example>
    /// <param name="men">The first value. The first bit of this value will appear first.</param>
    /// <param name="ladies">The second value. The first bit of this value will appear second.</param>
    /// <exception cref="IntercalException">
    /// Throws <see cref="IntercalError.E533"/> if the output is greater than the unsigned 32-bit integer maximum.
    /// </exception>
    /// <returns>The two mingled 16-bit numbers as a 32-bit numbers.</returns>
    [Pure] public static uint Mingle(uint men, uint ladies)
    {
        var a = (ushort)men;
        var b = (ushort)ladies;

        ulong retval = 0;

        for (var i = 15; i >= 0; i--)
        {
            if ((a & (ushort)Bitflags[i]) != 0)
                retval |= Bitflags[2 * i + 1];

            if ((b & (ushort)Bitflags[i]) != 0)
                retval |= Bitflags[2 * i];
        }
            
        if (retval > uint.MaxValue)
            Fail(IntercalError.E533);

        // ReSharper disable once IntVariableOverflowInUncheckedContext
        return (uint)retval;
    }
        
    /// <summary>
    /// Given a base number <c>a</c> and a selector <c>b</c>, b chooses which bits to pack into a new output c,
    /// where c is a 16 or 32-bit number depending on how many bits are selected.
    /// </summary>
    /// <example>
    /// <code>
    /// DO :1 &lt;- #179~#201
    /// </code>
    /// <c>#179~#201</c> is equivalent to <c>10110011~11001001</c>, and <c>:1</c> will equal to 00001001,
    /// which equals 9. The second operand selects the 8th, 7th, 4th, and 1st bit, being 1, 0, 0, 1 respectively.
    /// </example>
    /// <param name="a">The base number.</param>
    /// <param name="b">The selector.</param>
    /// <returns>A 16 or 32-bit number padded with zeroes.</returns>
    [Pure] public static uint Select(uint a, uint b)
    {
        uint retval = 0;
        var bit = 0;

        for (var i = 0; i < 32; i++)
        {
            if ((b & Bitflags[i]) == 0) continue;
            if ((a & Bitflags[i]) != 0)
                retval |= Bitflags[bit];
            bit++;
        }

        return retval;
    }
        
    /// <inheritdoc cref="Select(uint, uint)"/>
    [Pure] public static ushort Select(ushort a, ushort b)
    {
        ushort retval = 0;
        var bit = 0;

        for (var i = 0; i < 16; i++)
        {
            if ((b & Bitflags[i]) == 0) continue;
            if ((a & Bitflags[i]) != 0)
                retval |= (ushort)Bitflags[bit];
            bit++;
        }

        return retval;
    }
        
    [Pure] private static uint Rotate(uint val)
    {
        var b = ((val & Bitflags[0]) != 0);
        val /= 2;
        if (b)
            val |= Bitflags[31];
        return val;
    }

    [Pure] private static ushort Rotate(ushort val)
    {
        var b = (val & Bitflags[0]) != 0;
        val /= 2;
        if (b) val |= 0x8000;
        return val;
    }

    [Pure] public static ushort Reverse(ushort val)
    {
        ushort retval = 0;
        for (var i = 0; i < 16; i++)
        {
            if ((val & Bitflags[i]) != 0)
                retval |= (ushort)Bitflags[15 - i];
        }
        return retval;
    }
        
    [Pure] public static uint And(uint val) => val < ushort.MaxValue ? UnaryAnd16((ushort)val) : UnaryAnd32(val);
    [Pure] public static uint UnaryAnd32(uint val) => val & Rotate(val);
    [Pure] public static ushort UnaryAnd16(ushort val) => (ushort)(val & Rotate(val));

    [Pure] public static uint Or(uint val) => val < ushort.MaxValue ? UnaryOr16((ushort)val) : UnaryOr32(val);
    [Pure] public static uint UnaryOr32(uint val) => val | Rotate(val);
    [Pure] public static ushort UnaryOr16(ushort val) => (ushort)(val | Rotate(val));

    [Pure] public static uint Xor(uint val) => val < ushort.MaxValue ? UnaryXor16((ushort)val) : UnaryXor32(val);
    [Pure] public static uint UnaryXor32(uint val) => val ^ Rotate(val);
    [Pure] public static ushort UnaryXor16(ushort val) => (ushort)(val ^ Rotate(val));

    public static int Rand(int n) => Random.Next(n);
        
    /// <summary>
    /// Call this to raise an exception. This really should be a method on the execution context, not in the utility
    /// library
    /// </summary>
    /// <param name="errcode">An error code from <see cref="IntercalError"/>.</param>
    /// <exception cref="IntercalException"><see cref="errcode"/> incapsulated in an IntercalException.</exception>
    public static void Fail(string errcode)
    {
        Trace.WriteLine(errcode);
        throw new IntercalException(errcode);
    }
}