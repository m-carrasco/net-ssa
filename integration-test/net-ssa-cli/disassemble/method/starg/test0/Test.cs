// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Void Test::Foo(System.Int32,System.Int32,System.Int32,System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: a0 = starg.s a [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: a1 = starg.s b [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: a2 = starg.s c [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: s1 = ldc.i4.0
// CHECK: {{.*}}: s0 = call System.Int32 System.DateTime::DaysInMonth(System.Int32,System.Int32) [s0, s1]
// CHECK: {{.*}}: a3 = starg.s d [s0]
// CHECK: {{.*}}: ret

using System;

public class Test
{
    public void Foo(int a, int b, int c, int d)
    {
        a = System.DateTime.DaysInMonth(0, 0);
        b = System.DateTime.DaysInMonth(0, 0);
        c = System.DateTime.DaysInMonth(0, 0);
        d = System.DateTime.DaysInMonth(0, 0);
    }
}
