// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: SSA_QUERY_BIN=%ssa-query %net-ssa-cli %T/Test.dll disassemble --ssa method "System.Int32 Test::Foo(System.Boolean)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

/*
CHECK:	IL_0000: s0 = 0
CHECK:	IL_0001: l0 = s0
CHECK:	IL_0002: s0 = a0
CHECK:	IL_0003: brfalse IL_0010 [s0]
CHECK:	IL_0008: s0 = ldc.i4.s 100
CHECK:	IL_000a: l0 = s0
CHECK:	IL_000b: br IL_0013
CHECK:	IL_0010: s0 = ldc.i4.s 10
CHECK:	IL_0012: l0 = s0
CHECK:	PHI_0000: s0 = phi [(s0,IL_000b),(s0,IL_0012)]
CHECK:	PHI_0001: l0 = phi [(l0,IL_000b),(l0,IL_0012)]

CHECK:	IL_0013: s0 = l0
CHECK:	IL_0014: ret s0
*/

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
