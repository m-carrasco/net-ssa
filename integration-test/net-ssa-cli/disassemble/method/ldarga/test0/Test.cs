// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Void Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = &a0
// CHECK: IL_0002: l0 = s0
// CHECK: IL_0003: s0 = l0
// CHECK: IL_0004: call System.Void Test::Bar(System.Int32&) [s0]
// CHECK: IL_0009: ret 

using System;

public class Test
{
    public static void Foo(int a)
    {
        ref int addr = ref a;
        Bar(ref addr);
    }

    public static void Bar(ref int x)
    {
        x = 10;
    }
}
