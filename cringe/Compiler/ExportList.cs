using System;
using System.Reflection;
using INTERCAL.Compiler.Exceptions;
using INTERCAL.Runtime;

namespace INTERCAL.Compiler;

/// <summary>
/// This is used to hold a list of entry points exported from an assembly.
/// </summary>
public class ExportList
{
	public readonly string AssemblyFile;
	public readonly Assembly Assembly;
	public readonly EntryPointAttribute[] EntryPoints;

	public ExportList(string assemblyFile)
	{
		try
		{
			AssemblyFile = assemblyFile;
			Assembly = Assembly.LoadFrom(assemblyFile);
			EntryPoints = (EntryPointAttribute[])Assembly.GetCustomAttributes(typeof(EntryPointAttribute), true);
		}
		catch (Exception e)
		{
			throw new CompilationException(IntercalError.E2002, e);
		}
	}
}