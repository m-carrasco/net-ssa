using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetSsa.Analyses;
using NetSsa.Facts;
using NetSsa.Instructions;
using NetSsa.Queries;
using NUnit.Framework;

namespace UnitTest
{
    public class Tests
    {
        private AssemblyDefinition currentAssembly = null;
        private List<MethodDefinition> definedMethods;

        [SetUp]
        public void Setup()
        {
            string fullPath = System.Reflection.Assembly.GetAssembly(typeof(Tests)).Location;
            this.currentAssembly = AssemblyDefinition.ReadAssembly(fullPath);
            var definedTypes = this.currentAssembly.MainModule.Types;
            this.definedMethods = definedTypes.Select(type => type.Methods).SelectMany(l => l).ToList();
        }

        [TearDown]
        public void Exit()
        {
            this.currentAssembly.Dispose();
        }

        private static bool TestPhiCode(int a)
        {
            // Bytecode generated in debug
            /*
                IL_0000: nop
                IL_0001: ldarg.0
                IL_0002: ldc.i4.0
                IL_0003: cgt
                IL_0005: stloc.1
                IL_0006: ldloc.1
                IL_0007: brfalse.s IL_000d
                IL_0009: ldc.i4.1
                IL_000a: stloc.0
                IL_000b: br.s IL_000f
                IL_000d: ldc.i4.0
                IL_000e: stloc.0
                IL_000f: ldloc.0
                IL_0010: stloc.2
                IL_0011: br.s IL_0013
                IL_0013: ldloc.2
                IL_0014: ret
            */

            bool r;

            if (a > 0) r = true;
            else r = false;

            return r;
        }

        [Test]
        public void TestEdge()
        {
            var methodDefinition = definedMethods.Where(method => method.Name.Contains("TestPhiCode")).Single();
            var computedEdge = SsaFacts.Successor(methodDefinition.Body);
            var expected = new List<(String, String)>() {
                ("IL_0000","IL_0001"),
                ("IL_0001","IL_0002"),
                ("IL_0002","IL_0003"),
                ("IL_0003","IL_0005"),
                ("IL_0005","IL_0006"),
                ("IL_0006","IL_0007"),
                ("IL_0007","IL_000d"),
                ("IL_0007","IL_0009"),
                ("IL_0009","IL_000a"),
                ("IL_000a","IL_000b"),
                ("IL_000b","IL_000f"),
                ("IL_000d","IL_000e"),
                ("IL_000e","IL_000f"),
                ("IL_000f","IL_0010"),
                ("IL_0010","IL_0011"),
                ("IL_0011","IL_0013"),
                ("IL_0013","IL_0014"),
            };

            foreach (var inst in methodDefinition.Body.Instructions)
                System.Console.WriteLine(inst);

            Assert.That(computedEdge, Is.EquivalentTo(expected));
        }

        [Test]
        public void TestStart()
        {
            var methodDefinition = definedMethods.Where(method => method.Name.Contains("TestPhiCode")).Single();
            Assert.AreEqual(SsaFacts.Start(methodDefinition.Body), new Tuple<String>("IL_0000"));
        }

        [Test]
        public void TestVarDef()
        {
            var methodDefinition = definedMethods.Where(method => method.Name.Contains("TestPhiCode")).Single();
            var computedVarDef = SsaFacts.VarDef(methodDefinition.Body);
            var expected = new List<(String, String)>() {
                ("s0","IL_0001"),
                ("s0","IL_000d"),
                ("s0","IL_0013"),
                ("s0","IL_000f"),
                ("s0","IL_0003"),
                ("s0","IL_0006"),
                ("s0","IL_0009"),
                ("s1","IL_0002"),
            };

            Assert.That(computedVarDef, Is.EquivalentTo(expected));
        }

        [Test]
        public void TestSsaQuery()
        {
            var methodDefinition = definedMethods.Where(method => method.Name.Contains("TestPhiCode")).Single();
            var body = methodDefinition.Body;

            var varDef = SsaFacts.VarDef(body);
            var successor = SsaFacts.Successor(body);

            SsaQuery.Query(successor, varDef, out IEnumerable<(String, String)> phiLocation, out IEnumerable<(String, String)> dominators, out IEnumerable<(String, String)> domFrontier, out IEnumerable<(String, String)> edge);

            Assert.AreEqual(phiLocation.Count(), 1);
        }

        [Test]
        public void TestExampleDisassemble()
        {
            // If this test is changed, please update README accordingly.

            Mono.Cecil.MethodDefinition methodDefinition = definedMethods.Where(method => method.Name.Contains("TestPhiCode")).Single();
            Mono.Cecil.Cil.MethodBody body = methodDefinition.Body;

            LinkedList<NetSsa.Instructions.BytecodeInstruction> tac = Bytecode.Compute(body, out List<Variable> variables, out Dictionary<Mono.Cecil.Cil.Instruction, List<Variable>> uses, out Dictionary<Mono.Cecil.Cil.Instruction, List<Variable>> definitions);

            foreach (var ins in tac)
            {
                Mono.Cecil.Cil.Instruction cil = ins.Bytecode;
                Console.WriteLine("Opcode: " + cil.OpCode.Code);

                foreach (NetSsa.Analyses.Variable op in ins.Operands)
                {
                    Console.WriteLine("Operand: " + op.Name);
                }

                if (ins.Result != null)
                {
                    Console.WriteLine("Result: " + ins.Result.Name);
                }
            }
        }
    }
}
