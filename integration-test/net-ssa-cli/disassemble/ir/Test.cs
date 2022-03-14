// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble --type IR method "System.Int32 Test::Factorial(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: bge [[IL_0009:.*]] [s0, s1]
// CHECK: {{.*}}: s0 = ldc.i4.1
// CHECK: {{.*}}: ret [s0]
// CHECK: [[IL_0009]]: s0 = ldc.i4.1
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldc.i4.1
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: {{.*}}: br [[IL_001a:.*]]
// CHECK: [[IL_0012:.*]]: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: s1 = ldloc.1 [l1]
// CHECK: {{.*}}: s0 = mul [s0, s1]
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: s1 = ldc.i4.1
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: [[IL_001a]]: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: s1 = ldarg.0 [a0]
// CHECK: {{.*}}: ble [[IL_0012]] [s0, s1]
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: ret [s0]

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
