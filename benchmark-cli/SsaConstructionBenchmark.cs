using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NetSsa.Analyses;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace MyBenchmarks
{
    public abstract class SsaConstructionBenchmark
    {
        protected static readonly String MscorlibPath = "/usr/lib/mono/4.5/mscorlib.dll";
        protected static AssemblyDefinition Assembly = AssemblyDefinition.ReadAssembly(MscorlibPath);

        public SsaConstructionBenchmark() { }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            Assembly.Dispose();
            Assembly = null;
        }

        public SsaBody Dissassemble(MethodBody body)
        {
            BytecodeBody bytecodeBody = Bytecode.Compute(body);
            IR.VariableDefinitionsToUses(bytecodeBody);
            return Ssa.Compute(body.Method, bytecodeBody);
        }

        /*public List<SsaBody> DisassembleAll()
        {
            var res = new List<SsaBody>();

            foreach (var type in assembly.MainModule.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        BytecodeBody bytecodeBody = Bytecode.Compute(method.Body);
                        IR.VariableDefinitionsToUses(bytecodeBody);
                        SsaBody ssaBody = Ssa.Compute(method, bytecodeBody);
                        res.Add(ssaBody);
                    }
                }
            }

            return res;
        }*/
    }
    public class SsaByInstructionSizeBenchmark : SsaConstructionBenchmark
    {

        [ParamsSource(nameof(SizeBodies))]
        public BodyWrapper SizeBody { get; set; }

        public static IEnumerable<BodyWrapper> SizeBodies()
        {
            if (Assembly == null)
            {
                Assembly = AssemblyDefinition.ReadAssembly(MscorlibPath);
            }

            var it = new Iterator(Assembly);
            return it.FilterBodies(body => body.Instructions.Count);
        }

        [Benchmark]
        public SsaBody DisassembleBySize()
        {
            return Dissassemble(SizeBody.Body);
        }
    }

    public class SsaByEdgeBenchmark : SsaConstructionBenchmark
    {
        [ParamsSource(nameof(EdgeBodies))]
        public BodyWrapper EdgeBody { get; set; }

        public static IEnumerable<BodyWrapper> EdgeBodies()
        {
            if (Assembly == null)
            {
                Assembly = AssemblyDefinition.ReadAssembly(MscorlibPath);
            }

            var it = new Iterator(Assembly);
            var CountEdges = (MethodBody body) =>
            {
                IDictionary<Instruction, ISet<Instruction>> edges = new Dictionary<Instruction, ISet<Instruction>>();
                Successor.NonExceptionalSuccessor(edges, body);
                return edges.Select(kv => kv.Value.Count).Sum();
            };
            return it.FilterBodies(body => CountEdges(body));
        }

        [Benchmark]
        public SsaBody DisassembleByEdges()
        {
            return Dissassemble(EdgeBody.Body);
        }
    }
}
