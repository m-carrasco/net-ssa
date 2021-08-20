// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = a0
// CHECK: IL_0001: call System.Void System.Console::WriteLine(System.Object) [s0]
// CHECK: IL_0006: s0 = a1
// CHECK: IL_0007: ret s0

using System;

public class Test
{
    public int Foo(int a)
    {
        Console.WriteLine(this);
        return a;
    }
}
