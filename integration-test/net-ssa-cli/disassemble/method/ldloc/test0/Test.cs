// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo()" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = 0
// CHECK: IL_0001: s1 = 0
// CHECK: IL_0002: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_0007: l0 = s0
// CHECK: IL_0008: s0 = 0
// CHECK: IL_0009: s1 = 0
// CHECK: IL_000a: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_000f: l1 = s0
// CHECK: IL_0010: s0 = 0
// CHECK: IL_0011: s1 = 0
// CHECK: IL_0012: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_0017: l2 = s0
// CHECK: IL_0018: s0 = 0
// CHECK: IL_0019: s1 = 0
// CHECK: IL_001a: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_001f: l3 = s0
// CHECK: IL_0020: s0 = 0
// CHECK: IL_0021: s1 = 0
// CHECK: IL_0022: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_0027: l4 = s0
// CHECK: IL_0029: s0 = "boom"
// CHECK: IL_002e: call System.Void System.Console::WriteLine(System.String) [s0]
// CHECK: IL_0033: leave IL_005d
// CHECK: IL_0038: pop [e0]
// CHECK: IL_0039: s0 = l0
// CHECK: IL_003a: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: IL_003f: s0 = l1
// CHECK: IL_0040: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: IL_0045: s0 = l2
// CHECK: IL_0046: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: IL_004b: s0 = l3
// CHECK: IL_004c: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: IL_0051: s0 = l4
// CHECK: IL_0053: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: IL_0058: leave IL_005d
// CHECK: IL_005d: s0 = l0
// CHECK: IL_005e: ret s0

using System;

public class Test
{
    public int Foo()
    {
        int a = System.DateTime.DaysInMonth(0, 0);
        int b = System.DateTime.DaysInMonth(0, 0);
        int c = System.DateTime.DaysInMonth(0, 0);
        int d = System.DateTime.DaysInMonth(0, 0);
        int e = System.DateTime.DaysInMonth(0, 0);

        try
        {
            Console.WriteLine("boom");
        }
        catch (Exception)
        {
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);
            Console.WriteLine(d);
            Console.WriteLine(e);
        }

        return a;
    }
}
