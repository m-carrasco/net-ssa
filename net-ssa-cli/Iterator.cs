using System;
using System.IO;
using Mono.Cecil;
using System.Linq;

namespace NetSsaCli
{
    class Iterator
    {
        public static void IterateTypes(FileInfo input, Action<TypeDefinition> consumer)
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input.FullName))
            {
                foreach (TypeDefinition t in assembly.MainModule.GetTypes())
                {
                    consumer.Invoke(t);
                }
            }
        }

        public static void IterateMethods(FileInfo input, Action<MethodDefinition> consumer)
        {
            IterateTypes(input, t =>
            {
                foreach (MethodDefinition m in t.Methods)
                {
                    consumer.Invoke(m);
                }
            });
        }
    }
}