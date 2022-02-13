using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace MyBenchmarks
{
    public class Iterator
    {
        private readonly AssemblyDefinition assembly;
        public Iterator(AssemblyDefinition assembly)
        {
            this.assembly = assembly;
        }
        private MethodDefinition[] RandomMethodDefinitions()
        {
            Random rnd = new Random(93227202);
            return assembly.MainModule.GetTypes().Select(t => t.Methods).SelectMany(l => l).ToArray().OrderBy(x => rnd.Next()).ToArray();
        }

        public IEnumerable<MethodBody> IterateBodies()
        {
            var methods = RandomMethodDefinitions();

            foreach (var method in methods)
            {
                if (!method.HasBody)
                    continue;
                yield return method.Body;
            }
        }

        public IEnumerable<BodyWrapper> FilterBodies<T>(Func<MethodBody, T> prop)
        {
            var used = new HashSet<T>();
            foreach (var body in IterateBodies())
            {
                T p = prop(body);
                if (!used.Contains(p))
                {
                    used.Add(p);
                    Console.WriteLine(body.Method.FullName + ": " + p.ToString());
                    yield return new BodyWrapper(b => p.ToString()) { Body = body };
                }
            }
        }
    }
}
