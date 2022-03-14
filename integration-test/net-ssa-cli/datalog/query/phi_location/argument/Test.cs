// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN:  %net-ssa-cli %T/Test.dll datalog phi_location method "System.Int32 Test::Foo(System.Int32)" > %t.phi_location
// RUN: %FileCheck %s < %t.phi_location

// CHECK: phi_location:
// CHECK:   s0 IL_000d
// CHECK:   s1 IL_000d

public class Test
{
    public static int Foo(int a)
    {
        if (a < 0)
            a = a * -1;

        return a;
    }
}
