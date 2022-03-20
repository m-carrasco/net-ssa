// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo()" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

/*
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: l2 = stloc.2 [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: l3 = stloc.3 [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: l4 = stloc.s V_4 [s0]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: s0 = ldstr "boom"
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.String) [s0]
// CHECK: {{.*}}: leave [[L_0029:.*]]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: label
// CHECK: {{.*}}: pop [e0]
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: {{.*}}: s0 = ldloc.2 [l2]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: {{.*}}: s0 = ldloc.3 [l3]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: {{.*}}: s0 = ldloc.s V_4 [l4]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: {{.*}}: leave [[L_0029]]
// CHECK: [[L_0029]]: label
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: ret  [s0]
*/


using System;

public class Test
{
    public int Foo()
    {
        int a = System.DateTime.DaysInMonth(0, 0);
        int b = System.DateTime.DaysInMonth(0, 0);
        int c = System.DateTime.DaysInMonth(0, 0);
        int d = System.DateTime.DaysInMonth(0, 0);
        int e = System.DateTime.DaysInMonth(0, 0);

        try
        {
            Console.WriteLine("boom");
        }
        catch (Exception)
        {
            Console.WriteLine(a);
            Console.WriteLine(b);
            Console.WriteLine(c);
            Console.WriteLine(d);
            Console.WriteLine(e);
        }

        return a;
    }
}
