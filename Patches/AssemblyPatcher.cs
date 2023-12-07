using System.Collections.Generic;
using Mono.Cecil;

public static class AssemblyPatcher
{
    // List of assemblies to patch
    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    // Patches the assemblies
    public static void Patch(AssemblyDefinition assembly)
    {
        // Patcher code here

    }
}