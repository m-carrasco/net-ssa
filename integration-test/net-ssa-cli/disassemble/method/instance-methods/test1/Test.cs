// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo(System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Object) [s0]
// CHECK: {{.*}}: s0 = ldarg.1 [a1]
// CHECK: {{.*}}: s1 = ldarg.2 [a2]
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: s1 = ldarg.3 [a3]
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: s1 = ldarg.s d [a4]
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: s1 = ldarg.s e [a5]
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: s1 = ldarg.s f [a6]
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: s1 = ldarg.s g [a7]
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: ret [s0]

using System;

public class Test
{
    public int Foo(int a, int b, int c, int d, int e, int f, int g)
    {
        Console.WriteLine(this);
        return a + b + c + d + e + f + g;
    }
}
