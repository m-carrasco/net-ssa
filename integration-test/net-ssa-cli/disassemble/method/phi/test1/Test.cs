// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: SSA_QUERY_BIN=%ssa-query %net-ssa-cli %T/Test.dll disassemble --type Ssa method "System.Int32 Test::Foo(System.Boolean)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// RUN: SSA_QUERY_BIN=%ssa-query %net-ssa-cli %T/Test.dll datalog phi_location method "System.Int32 Test::Foo(System.Boolean)" > %t.phi_location

/*
CHECK: IL_0000: s0 = 0
CHECK: IL_0001: store l0, s0
CHECK: IL_0002: s0 = a0 [load]
CHECK: IL_0003: brfalse IL_0010 [s0]

CHECK: IL_0008: s0 = ldc.i4.s 10
CHECK: IL_000a: store l0, s0
CHECK: IL_000b: br IL_0016

CHECK: IL_0010: s0 = newobj System.Void System.Exception::.ctor()
CHECK: IL_0015: throw s0

CHECK: IL_0016: s0 = 5
CHECK: IL_0017: store l0, s0
CHECK: IL_0018: leave IL_0026

CHECK: IL_001d: store l1, e0
CHECK: IL_001e: s0 = ldc.i4.s 100
CHECK: IL_0020: store l0, s0
CHECK: IL_0021: leave IL_0026

CHECK: PHI_0000: s0 = phi [(s0,IL_0018),(s0,IL_0021)]
CHECK: IL_0026: s0 = l0 [load]
CHECK: IL_0027: ret s0
*/

using System;

public class Test
{
    public static int Foo(bool b)
    {
        int a = 0;

        try
        {
            if (b)
            {
                a = 10;
            }
            else
            {
                throw new Exception();
            }

            a = 5;

        }
        catch (Exception e)
        {
            a = 100;
        }

        return a;
    }
}
