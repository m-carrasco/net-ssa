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

        public SsaBody Dissassemble(MethodBody body)
        {
            BytecodeBody bytecodeBody = Bytecode.Compute(body);
            IR.VariableDefinitionsToUses(bytecodeBody);
            return Ssa.Compute(body.Method, bytecodeBody);
        }
    }

    public class SsaByInstructionSizeBenchmark : SsaConstructionBenchmark
    {

        [ParamsSource(nameof(SizeBodies))]
        public BodyWrapper SizeBody { get; set; }

        public static IEnumerable<BodyWrapper> SizeBodies()
        {
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

    public class SsaEntireAssembly : SsaConstructionBenchmark
    {
        public MethodBody[] InputBodies;
        public SsaBody[] OutputBodies;
        public SsaEntireAssembly()
        {
            var it = new Iterator(Assembly);
            InputBodies = it.IterateBodies().ToArray();
            OutputBodies = new SsaBody[InputBodies.Length];
        }

        [Benchmark]
        public SsaBody[] DissasembleAll()
        {
            for (uint i = 0; i < InputBodies.Length; i++)
            {
                OutputBodies[i] = Dissassemble(InputBodies[i]);
            }

            return OutputBodies;
        }
    }
}
