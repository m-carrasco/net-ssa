// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN:  %net-ssa-cli %T/Test.dll disassemble --type Ssa method "System.Int32 Test::Foo(System.Boolean)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK:	IL_0000: s0_0 = 0
// CHECK:	IL_0001: store l0, s0_0
// CHECK:	IL_0002: s0_1 = a0 [load]
// CHECK:	IL_0003: brfalse IL_0010 [s0_1]

// CHECK:	IL_0008: s0_3 = ldc.i4.s 100
// CHECK:	IL_000a: store l0, s0_3
// CHECK:	IL_000b: br IL_0013

// CHECK:	IL_0010: s0_2 = ldc.i4.s 10
// CHECK:	IL_0012: store l0, s0_2

// CHECK:	IL_0013: s0_5 = l0 [load]
// CHECK:	IL_0014: ret s0_5

using System;

public class Test
{
    public static int Foo(bool b)
    {
        int a = 0;

        if (b)
        {
            a = 100;
        }
        else
        {
            a = 10;
        }

        return a;
    }
}
