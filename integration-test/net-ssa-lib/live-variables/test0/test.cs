// RUN: %mcs -out:%T/Test.dll %S/program.cs.exclude
// RUN: (export BINARY_PATH=%T/Test.dll && dotnet repl --output-path %t.trx --run %s --exit-after-run)

#i "nuget:/tmp/build/bin/net-ssa/package"
#r "nuget: net-ssa-lib, 0.0.0"
#r "nuget: Mono.Cecil, 0.11.3"
#r "nuget: NUnit, 3.12.0"

using System;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

using NetSsa.Analyses;
using NetSsa.Instructions;

using NUnit.Framework;
using System.Linq;

public static String GetRegisters(IList<Register> registers, BitArray liveVariables){
    String result = "";
    for (int i=0; i < liveVariables.Length; i++){
        if (liveVariables[i]){
            result += registers[i].Name + " ";
        }
    }
    return result.Trim();
} 

AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(Environment.GetEnvironmentVariable("BINARY_PATH"));

var method = assembly.MainModule.GetType("HelloWorld").GetMethods().First();
Mono.Cecil.Cil.MethodBody body = method.Body;
IRBody irBody = Unstacker.Compute(body);
ControlFlowGraph cfg = new ControlFlowGraph(irBody);
LiveVariableAnalysis lva = new LiveVariableAnalysis(cfg);
lva.Flow();

/*
L_0000: label
L_0001: nop
L_0002: s0 = ldc.i4.0 
L_0003: l0 = stloc.0 [s0] -- s0
L_0004: br L_000c
L_0005: label
L_0006: s0 = ldloc.0 [l0]
L_0007: call System.Void System.Console::WriteLine(System.Int32) [s0] -- s0
L_0008: s0 = ldloc.0 [l0] 
L_0009: s1 = ldc.i4.1 ---- [s0]
L_000a: s0 = add [s0, s1] --- [s0, s1]
L_000b: l0 = stloc.0 [s0] --- [s0]
L_000c: label
L_000d: s0 = ldloc.0 [l0]
L_000e: s1 = ldc.i4.s 10 ---- [s0]
L_000f: blt L_0005 [s0, s1] -- s0 s1
L_0010: label
L_0011: ret
*/

Assert.AreEqual(irBody.Instructions.Count, 0x12);

Dictionary<int, string> results = new Dictionary<int, string>()
{
    { 0x0, "" },
    { 0x1, "" },
    { 0x2, "" },
    { 0x3, "s0" },
    { 0x4, "" },
    { 0x5, "" },
    { 0x6, "" },
    { 0x7, "s0" },
    { 0x8, "" },
    { 0x9, "s0" },
    { 0xa, "s0 s1" },
    { 0xb, "s0" },
    { 0xc, "" },
    { 0xd, "" },
    { 0xe, "s0" },
    { 0xf, "s0 s1" },
    { 0x10, "" },
    { 0x11, "" },
};

for (int i=0; i < 0x12; i++){
    Assert.AreEqual(results[i], GetRegisters(irBody.Registers, lva.IN[irBody.Instructions.ElementAt(i)]), i.ToString("x"));
}
