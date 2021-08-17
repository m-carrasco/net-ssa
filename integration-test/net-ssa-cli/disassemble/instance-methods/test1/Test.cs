// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble "System.Int32 Test::Foo(System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = a0
// CHECK: IL_0001: call System.Void System.Console::WriteLine(System.Object) [s0]
// CHECK: IL_0006: s0 = a1
// CHECK: IL_0007: s1 = a2
// CHECK: IL_0008: s0 = s0 + s1
// CHECK: IL_0009: s1 = a3
// CHECK: IL_000a: s0 = s0 + s1
// CHECK: IL_000b: s1 = a4
// CHECK: IL_000d: s0 = s0 + s1
// CHECK: IL_000e: s1 = a5
// CHECK: IL_0010: s0 = s0 + s1
// CHECK: IL_0011: s1 = a6
// CHECK: IL_0013: s0 = s0 + s1
// CHECK: IL_0014: s1 = a7
// CHECK: IL_0016: s0 = s0 + s1
// CHECK: IL_0017: ret s0


using System;

public class Test
{
    public int Foo(int a, int b, int c, int d, int e, int f, int g)
    {
        Console.WriteLine(this);
        return a + b + c + d + e + f + g;
    }
}
