// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: SSA_QUERY_BIN=%ssa-query %net-ssa-cli %T/Test.dll disassemble --type Ssa method "System.Int32 Test::Foo(System.Boolean)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// RUN: SSA_QUERY_BIN=%ssa-query %net-ssa-cli %T/Test.dll datalog phi_location method "System.Int32 Test::Foo(System.Boolean)" > %t.phi_location

// CHECK:	IL_0000: s0_0 = 0
// CHECK:	IL_0001: store l0, s0_0
// CHECK:	IL_0002: s0_2 = l0 [load]
// CHECK:	IL_0003: s1_1 = ldc.i4.3
// CHECK:	IL_0004: s0_3 = s0_2 * s1_1
// CHECK:	IL_0005: store l0, s0_3
// CHECK:	IL_0006: s0_4 = l0 [load]
// CHECK:	IL_0007: s1_2 = ldc.i4.s 100
// CHECK:	IL_0009: blt IL_0002 [s0_4, s1_2]
// CHECK:	IL_000e: s0_5 = l0 [load]
// CHECK:	IL_000f: ret s0_5

using System;

public class Test
{
    public static int Foo(bool b)
    {
        int a = 0;
        do
        {
            a = a * 3;
        } while (a < 100);

        return a;
    }
}
