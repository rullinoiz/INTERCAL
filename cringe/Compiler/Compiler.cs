using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Runtime;

namespace INTERCAL.Compiler
{
    internal static class Compiler
    {
        
        private static string PrepareSource(IEnumerable<string> files)
        {
            // First verify all files exist and have the right extension...
            string src = null;

            foreach (var file in files)
            {
                Trace.WriteLine($"Processing source file '{file}'");
                var dot = file.IndexOf('.');
                if (dot < 0)
                    throw new CompilationException(Messages.E998 + " (" + file + ")");

                var extension = file.Substring(dot);
                if (extension != ".i")
                    throw new CompilationException(Messages.E998 + " (" + file + ")");

                try
                {
                    var r = new StreamReader(file);
                    src += r.ReadToEnd();
                    r.Close();
                }

                catch (Exception e)
                {
                    Exception err = new CompilationException(Messages.E777 + " (" + file + ")", e);
                    throw err;
                }
            }
            return src;
        }

        private static void EmitBinary(CompilationContext c)
        {
            try
            {
                var writer = new StreamWriter("tmp.cs");
                writer.Write(c.ToString());
                writer.Close();
            }
            catch (Exception e)
            {
                throw new CompilationException(Messages.E888, e);
            }
            
            if (!c.Compile) return;

            var compiler = "csc";
            var userSpecifiedCompilerPath = ConfigurationManager.AppSettings["compilerPath"];
            if (!string.IsNullOrEmpty(userSpecifiedCompilerPath)) {
                compiler = userSpecifiedCompilerPath; 
            }
 
            string compilerArgs = null;

            if (c.DebugBuild)
            {
                compilerArgs = "/debug+ ";
            }

            switch (c.TypeOfAssembly)
            {
                case CompilationContext.AssemblyType.Winexe:
                case CompilationContext.AssemblyType.Exe:
                    compilerArgs += " /out:" + c.NameOfAssembly + ".exe ";
                    break;

                case CompilationContext.AssemblyType.Library:
                    compilerArgs += " /t:library /out:" + c.NameOfAssembly + ".dll ";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var needsComma = false;

            compilerArgs += " /r:";

            //We need to pass references down to the C# compiler
            if (c.References != null)
            {
                foreach (var t in c.References)
                {
                    if (needsComma)
                        compilerArgs += ",";

                    compilerArgs += '"' + t.AssemblyFile + '"';
                    needsComma = true;
                }
            }
            compilerArgs += " \"tmp.cs\"";

            try
            {
                Trace.WriteLine($"{compiler} {compilerArgs}");

                var si = new ProcessStartInfo(compiler, compilerArgs)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = Process.Start(si);
                if (p == null)
                {
                    Abort(Messages.E2003);
                    return;
                }
                p.WaitForExit();

                if (p.ExitCode == 0)
                {
                    CopyRequiredBinariesToOutputFolder(c);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Abort(Messages.E2003);
            }

            //File.Delete("~tmp.cs");
        }

        private static void CopyRequiredBinariesToOutputFolder(CompilationContext c)
        {
            Trace.WriteLine("Copying binaries to output folder...");
            foreach(var reference in c.References)
            {
                // note that we will skip files in the GAC
                if(File.Exists(reference.AssemblyFile))
                {
                    var sourceFileName = Path.GetFullPath(reference.AssemblyFile);
                    var destFileName = Path.Combine(Environment.CurrentDirectory, Path.GetFileName(sourceFileName));
                    Trace.WriteLine($"Copying '{sourceFileName}' to '{destFileName}'");

                    if(sourceFileName != destFileName)
                        File.Copy(sourceFileName, destFileName, true);
                }
                else
                {
                    Trace.WriteLine($"Not copying '{reference.AssemblyFile}' (File is in the GAC?)");
                }
            }
        }

        private const string EBanner =
            "Simple INTERCAL Compiler version {0}\r\n" +
            "for Microsoft (R) .NET Framework version {1}\r\n" +
            "Authorship disclaimed by Jason Whittington 2017. All rights reserved.\r\n\r\n";
        private const string EUsage =
            "                        SICK Compiler Options\r\n" +

            "                        - OUTPUT FILES -\r\n" +
            "/t:exe                  Build a console executable (default)\r\n" +
            "/t:library              Build a library \r\n" +

            "\r\n                      - INPUT FILES -\r\n" +
            "/r:<file list>          Reference metadata from the specified assembly files\r\n" +
            "/n                      Simply output the C# file" +

            "\r\n                      - CODE GENERATION -\r\n" +
            "/debug+                 Emit debugging information\r\n" +
            "/base:<class_name>      Use specified class as base class (e.g. MarshalByRefObject)\r\n" +
            "/public:<label_list>    Only emit stubs for the specified labels (ignored for .exe builds)\r\n" +
            "/traditional            Enforces the INTERCAL-72 standard operations." +

            "\r\n                      - ERRORS AND WARNINGS -\r\n" +
            "/b                      Reduce probably of E774 to zero.\r\n" +
            "/v or /verbose          Verbose compiler output\r\n";

        private const int EMinimumPoliteness = 20;
        private const int EMaximumPoliteness = 34;

        public static void Main(string[] args)
        {
            Console.WriteLine(EBanner, Assembly.GetExecutingAssembly().GetName().Version, Environment.Version);
            Trace.Listeners.Clear();

            try
            {
                switch (args.Length)
                {
                    case 0:
                        Abort(Messages.E777);
                        return;
                    case 1 when args[0].IndexOf("?", StringComparison.Ordinal) >= 0:
                        Console.WriteLine(EUsage);
                        return;
                }

                // Parse arguments...
                var c = new CompilationContext(args);
                var sources = new List<string>();

                foreach (var arg in args)
                {
                    if (arg[0] == '-' || arg[0] == '/')
                    {
                        if (arg.Substring(1).ToLower() == "v" || arg.Substring(1).ToLower() == "verbose")
                            Trace.Listeners.Add(new ConsoleTraceListener());
                        else if (arg.IndexOf("t:", StringComparison.Ordinal) == 1)
                            switch (arg.Substring(3))
                            {
                                case "library": c.TypeOfAssembly = CompilationContext.AssemblyType.Library; break;
                                case "exe": c.TypeOfAssembly = CompilationContext.AssemblyType.Exe; break;
                                default: Abort(Messages.E2001); break;
                            }

                        // using /r lets a programmer reference labels in another library, which allows DO NEXT
                        // to implicitly make calls into another component. 
                        else if (arg.IndexOf("r:", StringComparison.Ordinal) == 1)
                        {
                            var refs = arg.Substring(3).Split(',');
                            c.References = new ExportList[refs.Length + 1];

                            // For every referenced assembly we need to go drag out the labels exported
                            // by that assembly and store them on the context. NextStatement will use this 
                            // information to generate calls to the library.  In the case of duplicate labels
                            // behavior is undefined, chances are the first library listed with a matching label
                            // will be the one used.
                            for (var i = 0; i < refs.Length; i++)
                            {
                                Trace.WriteLine($"Referencing '{refs[i]}'");
                                c.References[i] = new ExportList(refs[i]);
                            }

                            // We put syslib in last. If other libs define labels that collide with
                            // syslibs then those will get precedence over the standard ones.
                            c.References[refs.Length] = new ExportList(FindFile("intercal.runtime.dll"));
                        }
                        else if (arg.IndexOf("DEBUG+", StringComparison.Ordinal) > 0 || arg.IndexOf("debug+", StringComparison.Ordinal) > 0)
                        {
                            Trace.WriteLine("Emitting a Debug build");
                            c.DebugBuild = true;
                        }

                        // this option can be used to control which labels to make public.
                        // If it is left off then all labels are made public.
                        // This option only makes sense when used with the /t:library option.
                        // It is ignored for .EXE builds.
                        else if (arg.IndexOf("public:", StringComparison.Ordinal) == 1)
                        {
                            c.PublicLabels = new Dictionary<string, bool>();
                            var labels = (arg.Substring(8)).Split(',');
                            foreach (var s in labels)
                                c.PublicLabels[s] = true;
                        }

                        // Let the user specify the base class.
                        // For example, setting the base class to System.Web.UI.Page allows the resulting assembly to be used as a codebehind assembly.
                        else if (arg.IndexOf("base:", StringComparison.Ordinal) == 1)
                        {
                            c.BaseClass = arg.Substring(6);
                            Trace.WriteLine($"Setting base type to {c.BaseClass}");
                        }
                        else if (arg.IndexOf("traditional", StringComparison.Ordinal) == 1)
                        {
                            c.Traditional = true;
                            Trace.WriteLine("Enabled traditional flag");
                        }

                        // /b reduces the probability of E774 to zero.
                        else if (arg.IndexOf("b", StringComparison.Ordinal) == 1)
                        {
                            Trace.WriteLine("(Intentional) Bugs disabled");
                            c.Buggy = false;
                        } 
                        else if (arg.IndexOf("n", StringComparison.Ordinal) == 1)
                        {
                            c.Compile = false;
                        }
                    }
                    else
                        sources.Add(arg);
                }

                // Auto-include standard lib if it hasn't been referenced already
                if (c.References == null && c.Compile)
                {
                    c.References = new ExportList[1];
                    var file = FindFile("intercal.runtime.dll");
                    c.References[0] = new ExportList(file);
                }
                
                // do the compilation
                var src = PrepareSource(sources);
                var fs = new StreamWriter("tmp.i");
                fs.Write(src);
                fs.Close();

                // Creating a program object parses it - any compile time errors will 
                // show up as an exception here.
                // If we do get an exception we purposely leave ~tmp.i sitting on the hard drive for the programer to inspect
                Trace.WriteLine("Parsing...");
                var p = Program.CreateFromFile("tmp.i");

                // Now do politeness checking. No point until we have at least three statements in the program.
                Trace.WriteLine("Analyzing Politeness...");
                if (p.StatementCount > 3)
                {
                    // less than 1/5 politeness level is not polite enough
                    if (p.Politeness < EMinimumPoliteness)
                        Abort(Messages.E079);
                    // more than 1/3 and you are too polite
                    else if (p.Politeness > EMaximumPoliteness)
                        Abort(Messages.E099);
                }
                
                c.Program = p;
                c.NameOfAssembly = Path.GetFileNameWithoutExtension(sources[0]);

                Trace.WriteLine("Emitting C#...");
                p.EmitCSharp(c);

                File.Delete("tmp.i");

                Trace.WriteLine("Emitting Binaries...");
                EmitBinary(c);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Abort(e.Message);
            }

        }

        private static string FindFile(string path)
        {
            if (File.Exists(path)) return path;
            
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Console.Write($"Checking {baseDir} for runtime...");
            if (baseDir != null && File.Exists(Path.Combine(baseDir, path))) 
                return Path.Combine(baseDir, path);
            throw new IntercalException(Messages.E2002);
        }

        private static void Abort(string error)
        {
            Console.WriteLine(error);
            Console.WriteLine("\tCORRECT SOURCE AND RESUBMIT");
        }
    }
}