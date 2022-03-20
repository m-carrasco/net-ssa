// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Factorial(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: bge [[L_0008:.*]] [s0, s1]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: s0 = ldc.i4.1
// CHECK: {{.*}}: ret  [s0]
// CHECK: [[L_0008]]: label
// CHECK: {{.*}}: s0 = ldc.i4.1
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldc.i4.1
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: {{.*}}: br [[L_0017:.*]]
// CHECK: [[L_000e:.*]]: label
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: s1 = ldloc.1 [l1]
// CHECK: {{.*}}: s0 = mul [s0, s1]
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: s1 = ldc.i4.1
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: [[L_0017]]: label
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: s1 = ldarg.0 [a0]
// CHECK: {{.*}}: ble [[L_000e]] [s0, s1]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: ret  [s0]

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
