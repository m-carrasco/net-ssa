// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble "System.Void Test::Foo()" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = 5
// CHECK: IL_0001: l0 = s0
// CHECK: IL_0002: s0 = &l0
// CHECK: IL_0004: l1 = s0
// CHECK: IL_0005: s0 = l1
// CHECK: IL_0006: call System.Void Test::Bar(System.Int32&) [s0]
// CHECK: IL_000b: s0 = l0
// CHECK: IL_000c: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: IL_0011: ret 

using System;

public class Test
{
    public static void Foo()
    {
        int a = 5;
        ref int addr = ref a;
        Bar(ref addr);
        Console.WriteLine(a);
    }

    public static void Bar(ref int x)
    {
        x = 10;
    }
}
