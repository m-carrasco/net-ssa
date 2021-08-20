// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Void Test::Foo(System.Int32,System.Int32,System.Int32,System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = 0
// CHECK: IL_0001: s1 = 0
// CHECK: IL_0002: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_0007: a0 = s0
// CHECK: IL_0009: s0 = 0
// CHECK: IL_000a: s1 = 0
// CHECK: IL_000b: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_0010: a1 = s0
// CHECK: IL_0012: s0 = 0
// CHECK: IL_0013: s1 = 0
// CHECK: IL_0014: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_0019: a2 = s0
// CHECK: IL_001b: s0 = 0
// CHECK: IL_001c: s1 = 0
// CHECK: IL_001d: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: IL_0022: a3 = s0
// CHECK: IL_0024: ret 

using System;

public class Test
{
    public void Foo(int a, int b, int c, int d)
    {
        a = System.DateTime.DaysInMonth(0, 0);
        b = System.DateTime.DaysInMonth(0, 0);
        c = System.DateTime.DaysInMonth(0, 0);
        d = System.DateTime.DaysInMonth(0, 0);
    }
}
