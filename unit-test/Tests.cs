using NUnit.Framework;
using Mono.Cecil;
using System.Linq;
using NetSsa.Facts;
using System.Collections.Generic;
using System;
using NetSsa.Queries;
using NetSsa.Analyses;
using Mono.Cecil.Cil;
using NetSsa.Instructions;

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
            var computedEdge = SsaFacts.Edge(methodDefinition.Body);
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
                ("l0","IL_000a"),
                ("l0","IL_000e"),
                ("l1","IL_0005"),
                ("l2","IL_0010"),
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
            var edge = SsaFacts.Edge(body);
            var start = SsaFacts.Start(body);

            SsaQuery.Query(start, edge, varDef, out IEnumerable<(String, String)> phiLocation, out IEnumerable<(String, String)> dominators, out IEnumerable<(String, String)> domFrontier);

            var expectedPhiLocation = new List<(String, String)>(){
                ("l0","IL_000f")
            };

            Assert.That(phiLocation, Is.EquivalentTo(expectedPhiLocation));
        }

        private static int Factorial(int a)
        {
            // Bytecode generated in debug
            /*
                IL_0000: nop
                IL_0001: ldarg.0  
                IL_0002: ldc.i4.0 
                IL_0003: clt 
                IL_0005: stloc.1 
                IL_0006: ldloc.1 
                IL_0007: brfalse.s IL_000d

                IL_0009: ldc.i4.1
                IL_000a: stloc.2
                IL_000b: br.s IL_002e

                IL_000d: ldc.i4.1
                IL_000e: stloc.0
                IL_000f: ldc.i4.1
                IL_0010: stloc.3
                IL_0011: br.s IL_001d

                IL_0013: nop
                IL_0014: ldloc.0
                IL_0015: ldloc.3
                IL_0016: mul
                IL_0017: stloc.0
                IL_0018: nop
                IL_0019: ldloc.3
                IL_001a: ldc.i4.1
                IL_001b: add
                IL_001c: stloc.3

                // PHI s0
                IL_001d: ldloc.3
                IL_001e: ldarg.0
                IL_001f: cgt
                IL_0021: ldc.i4.0
                IL_0022: ceq
                IL_0024: stloc.s V_4
                IL_0026: ldloc.s V_4
                IL_0028: brtrue.s IL_0013

                IL_002a: ldloc.0
                IL_002b: stloc.2
                IL_002c: br.s IL_002e
                
                IL_002e: ldloc.2
                IL_002f: ret
            */

            if (a < 0)
                return 1;

            int r = 1;
            for (int i = 1; i <= a; i++)
            {
                r *= i;
            }

            return r;
        }


        [Test]
        public void TestTac()
        {
            var methodDefinition = definedMethods.Where(method => method.Name.Equals("Factorial")).Single();
            var body = methodDefinition.Body;

            VariableDefUse.Compute(body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);
            var edge = SsaFacts.Edge(body);

            List<TacInstruction> tac = ThreeAddressCode.Compute(body, variables, uses, definitions);
            return;
        }
    }
}