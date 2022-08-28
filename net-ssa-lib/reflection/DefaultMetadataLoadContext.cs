using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

public static class DefaultMetadataLoadContext{
    public static MetadataLoadContext BuildMetadataLoadContextCurrentRuntime(string dllpath){
        // Get the array of runtime assemblies.
        string[] runtimeAssemblies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        
        // Create the list of assembly paths consisting of runtime assemblies and the inspected assembly.
        var paths = new List<string>(runtimeAssemblies);
        paths.Add(dllpath);

        // Create PathAssemblyResolver that can resolve assemblies using the created list.
        var resolver = new PathAssemblyResolver(paths);
        var context = new MetadataLoadContext(resolver);

        if (context.CoreAssembly == null){
            throw new NotSupportedException("CoreAssembly cannot be null.");
        }

        return context;
    }
}