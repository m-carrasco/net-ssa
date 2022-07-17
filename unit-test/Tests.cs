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
        public void TestSsaVerifierSelfReferential()
        {
            // Only a phi node can be self referential
            IRBody irBody = new IRBody();
            irBody.ExceptionHandlers = new List<ExceptionHandlerEntry>();
            irBody.Instructions = new LinkedList<TacInstruction>();

            BytecodeInstruction inst = new BytecodeInstruction(OpCodes.Nop, null);
            Register result = new Register("dummy");
            inst.Result = result;
            result.AddDefinition(inst);
            result.AddUse(inst);
            inst.Node = irBody.Instructions.AddFirst(inst);

            SsaVerifier ssaVerifier = new SsaVerifier(irBody);
            VerifierException exception = Assert.Throws<VerifierException>(() => ssaVerifier.Verify());
            Assert.IsTrue(exception.ToString().Contains("self referential"));
        }

        [Test]
        public void TestSsaVerifierOnlyOneDef()
        {
            IRBody irBody = new IRBody();
            irBody.ExceptionHandlers = new List<ExceptionHandlerEntry>();
            irBody.Instructions = new LinkedList<TacInstruction>();

            BytecodeInstruction inst = new BytecodeInstruction(OpCodes.Nop, null);
            Register result = new Register("dummy0");
            result.AddDefinition(inst);
            inst.Node = irBody.Instructions.AddLast(inst);

            inst = new BytecodeInstruction(OpCodes.Nop, null);
            result.AddDefinition(inst);
            inst.Node = irBody.Instructions.AddLast(inst);

            SsaVerifier ssaVerifier = new SsaVerifier(irBody);
            VerifierException exception = Assert.Throws<VerifierException>(() => ssaVerifier.Verify());
            Assert.IsTrue(exception.ToString().Contains("is defined twice"));
        }

        [Test]
        public void TestSsaVerifierUseDomByDef()
        {
            IRBody irBody = new IRBody();
            irBody.ExceptionHandlers = new List<ExceptionHandlerEntry>();
            irBody.Instructions = new LinkedList<TacInstruction>();

            BytecodeInstruction nop = new BytecodeInstruction(OpCodes.Nop, null);
            ControlFlowInstruction br = new ControlFlowInstruction(OpCodes.Br);
            BytecodeInstruction def = new BytecodeInstruction(OpCodes.Nop, null);
            LabelInstruction label = new LabelInstruction();
            BytecodeInstruction use = new BytecodeInstruction(OpCodes.Nop, null);

            nop.Node = irBody.Instructions.AddLast(nop);
            br.Node = irBody.Instructions.AddLast(br);
            def.Node = irBody.Instructions.AddLast(def);
            label.Node = irBody.Instructions.AddLast(label);
            use.Node = irBody.Instructions.AddLast(use);

            Register result = new Register("dummy0");
            result.AddDefinition(def);
            br.Targets.Add(label);
            result.AddUse(use);

            SsaVerifier ssaVerifier = new SsaVerifier(irBody);
            VerifierException exception = Assert.Throws<VerifierException>(() => ssaVerifier.Verify());
            Assert.IsTrue(exception.ToString().Contains("is not dominated"));
        }


        [Test]
        public void TestSsaVerifierPhiOperandsEqualsPredecessors()
        {
            IRBody irBody = new IRBody();
            irBody.ExceptionHandlers = new List<ExceptionHandlerEntry>();
            irBody.Instructions = new LinkedList<TacInstruction>();

            BytecodeInstruction nop0 = new BytecodeInstruction(OpCodes.Nop, null);
            ControlFlowInstruction br = new ControlFlowInstruction(OpCodes.Brfalse_S);
            BytecodeInstruction nop1 = new BytecodeInstruction(OpCodes.Nop, null);
            LabelInstruction label = new LabelInstruction();
            PhiInstruction phi = new PhiInstruction();
            BytecodeInstruction nop2 = new BytecodeInstruction(OpCodes.Nop, null);

            nop0.Node = irBody.Instructions.AddLast(nop0);
            br.Node = irBody.Instructions.AddLast(br);
            nop1.Node = irBody.Instructions.AddLast(nop1);
            label.Node = irBody.Instructions.AddLast(label);
            phi.Node = irBody.Instructions.AddLast(phi);
            nop2.Node = irBody.Instructions.AddLast(nop2);

            Register res0 = new Register("dummy0");
            res0.AddDefinition(nop0);

            br.Targets.Add(label);

            Register res1 = new Register("dummy1");
            res1.AddDefinition(nop1);

            Register res2 = new Register("dummy2");
            res2.AddDefinition(phi);
            res0.AddUse(phi);
            phi.Incoming.Add(br);

            SsaVerifier ssaVerifier = new SsaVerifier(irBody);
            VerifierException exception = Assert.Throws<VerifierException>(() => ssaVerifier.Verify());
            Assert.IsTrue(exception.ToString().Contains("Phi instruction with a different amount of operands"));
        }


        [Test]
        public void TestSsaVerifierPhiFirstInBasicBlock()
        {
            IRBody irBody = new IRBody();
            irBody.ExceptionHandlers = new List<ExceptionHandlerEntry>();
            irBody.Instructions = new LinkedList<TacInstruction>();

            BytecodeInstruction nop0 = new BytecodeInstruction(OpCodes.Nop, null);
            ControlFlowInstruction br = new ControlFlowInstruction(OpCodes.Brfalse_S);
            BytecodeInstruction nop1 = new BytecodeInstruction(OpCodes.Nop, null);
            LabelInstruction label = new LabelInstruction();
            BytecodeInstruction nop2 = new BytecodeInstruction(OpCodes.Nop, null);
            PhiInstruction phi = new PhiInstruction();
            BytecodeInstruction nop3 = new BytecodeInstruction(OpCodes.Nop, null);

            nop0.Node = irBody.Instructions.AddLast(nop0);
            br.Node = irBody.Instructions.AddLast(br);
            nop1.Node = irBody.Instructions.AddLast(nop1);
            label.Node = irBody.Instructions.AddLast(label);
            nop2.Node = irBody.Instructions.AddLast(nop2);
            phi.Node = irBody.Instructions.AddLast(phi);
            nop3.Node = irBody.Instructions.AddLast(nop3);

            Register res0 = new Register("dummy0");
            res0.AddDefinition(nop0);

            br.Targets.Add(label);

            Register res1 = new Register("dummy1");
            res1.AddDefinition(nop1);

            Register res2 = new Register("dummy2");
            res2.AddDefinition(phi);
            res0.AddUse(phi);
            res1.AddUse(phi);
            phi.Incoming.Add(br);
            phi.Incoming.Add(nop1);

            SsaVerifier ssaVerifier = new SsaVerifier(irBody);
            VerifierException exception = Assert.Throws<VerifierException>(() => ssaVerifier.Verify());
            Assert.IsTrue(exception.ToString().Contains("PHIs must be the first thing in a basic block"));
        }

        [Test]
        public void TestExampleDisassemble()
        {
            // If this test is changed, please update README accordingly.

            Mono.Cecil.MethodDefinition methodDefinition = definedMethods.Where(method => method.Name.Contains("TestPhiCode")).Single();
            Mono.Cecil.Cil.MethodBody body = methodDefinition.Body;

            IRBody irBody = Unstacker.Compute(body);
            // This call is optional.
            Ssa.Compute(irBody);
            // This analysis is optional and it requires SSA
            StackTypeInference analysis = new StackTypeInference(irBody);
            IDictionary<Register, StackType> stackTypes = analysis.Type();

            foreach (NetSsa.Instructions.TacInstruction ins in irBody.Instructions)
            {
                Console.WriteLine(ins);
                if (ins.Result is Register register){
                    Console.WriteLine(stackTypes[register]);
                }
            }
        }
    }
}
