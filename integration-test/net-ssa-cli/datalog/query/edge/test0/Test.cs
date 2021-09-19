// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: SSA_QUERY_BIN=%ssa-query %net-ssa-cli %T/Test.dll datalog edge method "System.Int32 Test::Foo(System.Int32)" > %t.edge
// RUN: %FileCheck %s < %t.edge

// CHECK: edge: 
// CHECK: 	IL_0000 IL_0001
// CHECK: 	IL_0000 IL_0011
// CHECK: 	IL_0000 IL_001c
// CHECK: 	IL_0001 IL_000c
// CHECK: 	IL_0001 IL_0006
// CHECK: 	IL_0001 IL_0011
// CHECK: 	IL_0001 IL_001c
// CHECK: 	IL_0006 IL_000b
// CHECK: 	IL_0006 IL_0011
// CHECK: 	IL_0006 IL_001c
// CHECK: 	IL_000b IL_0011
// CHECK: 	IL_000b IL_001c
// CHECK: 	IL_000c IL_0022
// CHECK: 	IL_000c IL_0011
// CHECK: 	IL_000c IL_001c
// CHECK: 	IL_0011 IL_0012
// CHECK: 	IL_0011 IL_001c
// CHECK: 	IL_0012 IL_0013
// CHECK: 	IL_0012 IL_001c
// CHECK: 	IL_0013 IL_0014
// CHECK: 	IL_0013 IL_001c
// CHECK: 	IL_0014 IL_0015
// CHECK: 	IL_0014 IL_001c
// CHECK: 	IL_0015 IL_0017
// CHECK: 	IL_0015 IL_001c
// CHECK: 	IL_0017 IL_0022
// CHECK: 	IL_0017 IL_001c
// CHECK: 	IL_001c IL_001d
// CHECK: 	IL_001d IL_001e
// CHECK: 	IL_001e IL_001f
// CHECK: 	IL_001f IL_0021
// CHECK: 	IL_0021 IL_0022
// CHECK: 	IL_0022 IL_0023


using System;

public class Test
{
    public static int Foo(int a)
    {
        try
        {
            if (a == 0)
            {
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            a++;
        }
        finally
        {
            a++;
        }

        return a;
    }
}
