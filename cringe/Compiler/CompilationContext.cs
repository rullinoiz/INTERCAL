using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using INTERCAL.Expressions;
using INTERCAL.Extensions;
using INTERCAL.Runtime;
using INTERCAL.Statements;

namespace INTERCAL.Compiler;

public class CompilationContext
{
    public enum AssemblyType
    {
        Library, Exe, Winexe
    };

    public readonly string[] Arguments;

    private readonly StringBuilder _source = new StringBuilder();
        
    /// <summary>
    /// Whether or not to compile the transpiled INTERCAL program (/n is used to transpile the magic library).
    /// </summary>
    public bool Compile = true;
        
    /// <summary>
    /// This map is used to map abstains to labels in the runtime. The compiler is smart enough to only emit abstain
    /// guards for statements that might possibly be abstained. At least I think it is - maybe that's a source of
    /// bugs... 
    /// </summary>
    public static readonly Dictionary<string, Type> AbstainMap = new Dictionary<string, Type>();

    /// <summary>
    /// What program are we compiling?
    /// </summary>
    public Program Program;

    /// <summary>
    /// What is the build target? 
    /// </summary>
    public string NameOfAssembly;
    public AssemblyType TypeOfAssembly = AssemblyType.Exe;
    public bool DebugBuild;
    public bool Verbose = false;

    /// <summary>
    /// What will the base class be for the generated type?
    /// </summary>
    public string BaseClass = "System.Object";

    /// <summary>
    /// Which assemblies are we referencing?
    /// </summary>
    public ExportList[] References;
        
    /// <summary>
    /// Which labels in this assembly will be turned into public entry points?
    /// </summary>
    public Dictionary<string, bool> PublicLabels;

    /// <summary>
    /// Public PRNG, mostly used for <see cref="IntercalError.E774"/>.
    /// </summary>
    public readonly Random Random = new Random();

    /// <summary>
    /// If this is set to false then <see cref="IntercalError.E774"/> is never emitted.
    /// </summary>
    public bool Buggy = true;
        
    /// <summary>
    /// Enforces the INTERCAL-72 standard operations.
    /// </summary>
    public bool Traditional = false;
        
    /// <summary>
    /// How many tabs we are inside of? (to make the output look pretty)
    /// </summary>
    private int _depth;
        
    /// <summary>
    /// If this program references external instance classes I don't want to create a new one at every method call.
    /// Instead this compiler will emit properties that lazy-instantiate the requested classes. This List is filled
    /// up by NextStatement::EmitExternalCall and is then used to generate the private properties.
    /// </summary>
    public readonly List<string> ExternalReferences = new List<string>();
        
    public CompilationContext(string[] args)
    {
        Arguments = args;
    }
        
    static CompilationContext()
    {
        foreach (var type in  typeof(Statement).GetNestedTypes()
                     .Where(t=>t.GetField("GerundName") != null))
            AbstainMap[(string)type.GetField("GerundName")!.GetRawConstantValue()!] = type;
    }

    public override string ToString() => _source.ToString();

    public string Indent() => '\t'.Multiply(_depth);

    public CompilationContext BeginBlock()
    {
        _source.Append(Indent() + "{");
        _source.Append("\r\n");
        _depth++;
        return this;
    }

    public CompilationContext EndBlock()
    {
        _depth--;
        _source.Append(Indent() + "}");
        _source.Append("\r\n");
        return this;
    }

    public CompilationContext Emit(string s)
    {
        _source.Append(Indent() + s);
        _source.Append("\r\n");
        return this;
    }

    public CompilationContext EmitRaw(string s)
    {
        _source.Append(s);
        return this;
    }

    public static void Warn(string s) => Console.WriteLine("Warning: " + s);

    public static string GeneratePropertyName(string className)
    {
        var s = className.Split('.');
        return string.Join(null, s) + "Prop";
    }

    internal void EmitRaw(Expression depth)
    {
        throw new NotImplementedException();
    }
}