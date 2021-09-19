// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: SSA_QUERY_BIN=%ssa-query %net-ssa-cli %T/Test.dll datalog phi_location method "System.Int32 Test::Foo(System.Int32)" > %t.phi_location

// RUN: %FileCheck %s < %t.phi_location

// CHECK: phi_location: 
// CHECK: 	s0 IL_0011
// CHECK: 	s0 IL_001c
// CHECK: 	s0 IL_0022
// CHECK: 	l0 IL_001c
// CHECK: 	l0 IL_0022
// CHECK: 	s1 IL_001c
// CHECK: 	s1 IL_0022
// CHECK: 	a0 IL_001c
// CHECK: 	a0 IL_0022

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