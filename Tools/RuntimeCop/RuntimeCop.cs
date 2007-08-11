using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.GetOptions;

public class CopOptions : Options {
	public CopOptions (string [] args):
		base (args)
	{
	}
}

public class RuntimeCop {
	public RuntimeCop(CopOptions options)
	{
		this.options = options;

		if (this.options.RemainingArguments.Length == 0) {
			throw new ArgumentException ();
		}
		
		this.corlib = this.options.RemainingArguments [0];
	}

	CopOptions options;
	string corlib;
	List <MethodDefinition> internalStubs = new List<MethodDefinition> ();
	
	public int Run ()
	{
		AssemblyDefinition library;

		Console.WriteLine ("Loading `{0}'", corlib);
		library = AssemblyFactory.GetAssembly (corlib);

		foreach (ModuleDefinition module in library.Modules)
			ScanModule (module);

		Console.WriteLine ("Finished scanning.\n");
		
		foreach (MethodDefinition def in internalStubs)
			Console.WriteLine (def);

		return 0;
	}

	public void ScanModule (ModuleDefinition module)
	{
		foreach (TypeDefinition type in module.Types)
			ScanType (type);
	}

	public void ScanType (TypeDefinition type)
	{
		foreach (TypeDefinition nestedType in type.NestedTypes)
			ScanType (nestedType);
		
		foreach (MethodDefinition method in type.Methods)
			ScanMethod (method);
	}

	public void ScanMethod (MethodDefinition method)
	{
		if (method.IsInternalCall)
			internalStubs.Add (method);
	}
	
	public static int Main (string [] args)
	{
		
		RuntimeCop cop;

		try {
			cop = new RuntimeCop (new CopOptions (args));
		} catch (ArgumentException e) {
			Console.Error.WriteLine ("Bad arguments, see -help");
			return 1;
		}
		
		return cop.Run ();
	}
}