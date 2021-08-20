// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = -1
// CHECK: IL_0001: l0 = s0
// CHECK: IL_0002: s0 = l0
// CHECK: IL_0003: s1 = 1
// CHECK: IL_0004: s0 = s0 + s1
// CHECK: IL_0005: l0 = s0
// CHECK: IL_0006: leave IL_0030
// CHECK: IL_000b: s0 = isinst System.Exception [e0]
// CHECK: IL_0010: l1 = s0
// CHECK: IL_0011: s0 = l1
// CHECK: IL_0012: br IL_001a if s0 == true
// CHECK: IL_0014: s0 = 0
// CHECK: IL_0015: br IL_001e
// CHECK: IL_001a: s0 = l0
// CHECK: IL_001b: s1 = 2
// CHECK: IL_001c: s0 = s0 == s1
// CHECK: IL_001e: endfilter [s0]
// CHECK: IL_0020: pop [e1]
// CHECK: IL_0021: s0 = l0
// CHECK: IL_0022: s1 = 1
// CHECK: IL_0023: s0 = s0 + s1
// CHECK: IL_0024: l0 = s0
// CHECK: IL_0025: s0 = l1
// CHECK: IL_0026: call System.Void Test::Bar(System.Exception) [s0]
// CHECK: IL_002b: leave IL_0030
// CHECK: IL_0030: s0 = l0
// CHECK: IL_0031: ret s0


using System;

public class Test
{
    public static int Foo(int a)
    {
        int r = -1;
        try
        {
            r++;
        }
        catch (Exception exception) when (r == 2)
        {
            r++;
            Bar(exception);
        }

        return r;
    }

    public static void Bar(Exception exception) { }
}
