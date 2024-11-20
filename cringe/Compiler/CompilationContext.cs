using System;
using System.Collections.Generic;
using System.Text;
using INTERCAL.Expressions;
using INTERCAL.Statements;

namespace INTERCAL.Compiler
{
    public class CompilationContext
    {
        public enum AssemblyType
        {
            Library, Exe, Winexe
        };

        private readonly StringBuilder _source = new StringBuilder();
        
        /// <summary>
        /// This map is used to map abstains to labels in the runtime.
        /// The compiler is smart enough to only emit abstain guards for statements that might possibly be abstained.
        /// At least I think it is - maybe that's a source of bugs... 
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
        ///Which labels in this assembly will be turned into public entry points?
        /// </summary>
        public Dictionary<string, bool> PublicLabels;

        /// <summary>
        /// public PRNG, mostly used for E774
        /// </summary>
        public readonly Random Random = new Random();

        /// <summary>
        /// if this is set to false then E774 is never emitted
        /// </summary>
        public bool Buggy = true;
        
        /// <summary>
        /// if this program references external instance classes I don't want to create a new one at every method call.
        /// Instead this compiler will emit properties that lazy-instantiate the requested classes.
        /// This List is filled up by NextStatement::EmitExternalCall and is then used to generate the private properties.
        /// </summary>
        public readonly List<string> ExternalReferences = new List<string>();

        static CompilationContext()
        {
            AbstainMap["NEXTING"] = typeof(Statement.NextStatement);
            AbstainMap["FORGETTING"] = typeof(Statement.ForgetStatement);
            AbstainMap["RESUMING"] = typeof(Statement.ResumeStatement);
            AbstainMap["STASHING"] = typeof(Statement.StashStatement);
            AbstainMap["RETRIEVING"] = typeof(Statement.RetrieveStatement);
            AbstainMap["IGNORING"] = typeof(Statement.IgnoreStatement);
            AbstainMap["REMEMBERING"] = typeof(Statement.RememberStatement);
            AbstainMap["ABSTAINING"] = typeof(Statement.AbstainStatement);
            AbstainMap["REINSTATING"] = typeof(Statement.ReinstateStatement);
            AbstainMap["CALCULATING"] = typeof(Statement.CalculateStatement);
            AbstainMap["COMING FROM"] = typeof(Statement.ComeFromStatement);
        }

        public override string ToString() { return _source.ToString(); }

        public void Emit(string s)
        {
            _source.Append(s);
            _source.Append(";\r\n");
        }

        public void EmitRaw(string s)
        {
            _source.Append(s);
        }

        public static void Warn(string s)
        {
            Console.WriteLine("Warning: " + s);
        }
        
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
}