// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll datalog phi_location method "System.Int32 Test::Factorial(System.Int32)" > %t.phi_location
// RUN: %FileCheck %s < %t.phi_location

// CHECK: phi_location: 
// CHECK: 	s0 IL_001a
// CHECK: 	s1 IL_001a
public class Test
{
    public static int Factorial(int a)
    {
        if (a < 0)
            return 1;

        int r = 1;
        for (int i = 1; i <= a; i++)
        {
            r *= i;
        }

        return r;
    }
}
