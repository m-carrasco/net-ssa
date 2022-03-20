// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

/*
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: s0 = ldc.i4.m1
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: s1 = ldc.i4.1
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: s1 = ldc.i4.5
// CHECK: {{.*}}: bne.un [[L_000f:.*]] [s0, s1]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: s0 = newobj System.Void System.Exception::.ctor()
// CHECK: {{.*}}: throw  [s0]
// CHECK: [[L_000f]]: label
// CHECK: {{.*}}: leave [[L_0027:.*]]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: label
// CHECK: {{.*}}: l1 = stloc.1 [e0]
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: s1 = ldc.i4.1
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: call System.Void Test::Bar(System.Exception) [s0]
// CHECK: {{.*}}: leave [[L_0027]]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: label
// CHECK: {{.*}}: l2 = stloc.2 [e1]
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: s1 = ldc.i4.1
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldloc.2 [l2]
// CHECK: {{.*}}: call System.Void Test::Bar(System.Exception) [s0]
// CHECK: {{.*}}: leave [[L_0027]]
// CHECK: [[L_0027]]: label
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: ret  [s0]
*/

using System;

public class Test
{
    public static int Foo(int a)
    {
        int r = -1;
        try
        {
            r++;
            if (a == 5)
            {
                throw new System.Exception();
            }
        }
        catch (ArgumentException exception)
        {
            r++;
            Bar(exception);
        }
        catch (NullReferenceException exception)
        {
            r++;
            Bar(exception);
        }

        return r;
    }

    public static void Bar(Exception exception) { }
}
