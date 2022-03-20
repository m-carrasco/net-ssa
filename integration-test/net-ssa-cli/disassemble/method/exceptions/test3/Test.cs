// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

/*
//CHECK: {{.*}}: label
//CHECK: {{.*}}: nop
//CHECK: {{.*}}: s0 = ldc.i4.m1
//CHECK: {{.*}}: l0 = stloc.0 [s0]
//CHECK: {{.*}}: label
//CHECK: {{.*}}: s0 = ldloc.0 [l0]
//CHECK: {{.*}}: s1 = ldc.i4.1
//CHECK: {{.*}}: s0 = add [s0, s1]
//CHECK: {{.*}}: l0 = stloc.0 [s0]
//CHECK: {{.*}}: leave [[L_0024:.*]]
//CHECK: {{.*}}: label
//CHECK: {{.*}}: nop
//CHECK: {{.*}}: label
//CHECK: {{.*}}: s0 = isinst System.Exception [e0]
//CHECK: {{.*}}: l1 = stloc.1 [s0]
//CHECK: {{.*}}: s0 = ldloc.1 [l1]
//CHECK: {{.*}}: brtrue.s [[L_0014:.*]] [s0]
//CHECK: {{.*}}: label
//CHECK: {{.*}}: s0 = ldc.i4.0
//CHECK: {{.*}}: br L_0018
//CHECK: [[L_0014]]: label
//CHECK: {{.*}}: s0 = ldloc.0 [l0]
//CHECK: {{.*}}: s1 = ldc.i4.2
//CHECK: {{.*}}: s0 = ceq [s0, s1]
//CHECK: {{.*}}: label
//CHECK: {{.*}}: endfilter  [s0]
//CHECK: {{.*}}: label
//CHECK: {{.*}}: nop
//CHECK: {{.*}}: pop [e1]
//CHECK: {{.*}}: s0 = ldloc.0 [l0]
//CHECK: {{.*}}: s1 = ldc.i4.1
//CHECK: {{.*}}: s0 = add [s0, s1]
//CHECK: {{.*}}: l0 = stloc.0 [s0]
//CHECK: {{.*}}: s0 = ldloc.1 [l1]
//CHECK: {{.*}}: call System.Void Test::Bar(System.Exception) [s0]
//CHECK: {{.*}}: leave [[L_0024]]
//CHECK: [[L_0024]]: label
//CHECK: {{.*}}: s0 = ldloc.0 [l0]
//CHECK: {{.*}}: ret  [s0]
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
        }
        catch (Exception exception) when (r == 2)
        {
            r++;
            Bar(exception);
        }

        return r;
    }

    public static void Bar(Exception exception) { }
}
