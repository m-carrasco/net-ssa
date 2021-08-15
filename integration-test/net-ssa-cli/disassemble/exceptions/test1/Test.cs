// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble "System.Int32 Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = -1
// CHECK: IL_0001: l0 = s0
// CHECK: IL_0002: s0 = l0
// CHECK: IL_0003: s1 = 1
// CHECK: IL_0004: s0 = s0 + s1
// CHECK: IL_0005: l0 = s0
// CHECK: IL_0006: s0 = a0
// CHECK: IL_0007: s1 = 5
// CHECK: IL_0008: br IL_0013 if s0 != s1 [unsigned]
// CHECK: IL_000d: s0 = newobj System.Void System.Exception::.ctor()
// CHECK: IL_0012: throw s0
// CHECK: IL_0013: leave IL_002d
// CHECK: IL_0018: l1 = e0
// CHECK: IL_0019: s0 = l0
// CHECK: IL_001a: s1 = 1
// CHECK: IL_001b: s0 = s0 + s1
// CHECK: IL_001c: l0 = s0
// CHECK: IL_001d: s0 = l1
// CHECK: IL_001e: call System.Void Test::Bar(System.Exception) [s0]
// CHECK: IL_0023: leave IL_002d
// CHECK: IL_0028: s0 = l0
// CHECK: IL_0029: s1 = 1
// CHECK: IL_002a: s0 = s0 + s1
// CHECK: IL_002b: l0 = s0
// CHECK: IL_002c: endfinally
// CHECK: IL_002d: s0 = l0
// CHECK: IL_002e: ret s0

using System;

public class Test
{
    public static int Foo(int a)
    {
        int r = -1;
        try
        {
            r++;
            if (a == 5)
            {
                throw new System.Exception();
            }
        }
        catch (Exception exception)
        {
            r++;
            Bar(exception);
        }
        finally
        {
            r++;
        }

        return r;
    }

    public static void Bar(Exception exception) { }
}
