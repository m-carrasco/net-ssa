// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Void Test::Foo()" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: s0 = ldc.i4.5
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldloca.s V_0 [l0]
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: call System.Void Test::Bar(System.Int32&) [s0]
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Int32) [s0]
// CHECK: {{.*}}: ret

using System;

public class Test
{
    public static void Foo()
    {
        int a = 5;
        ref int addr = ref a;
        Bar(ref addr);
        Console.WriteLine(a);
    }

    public static void Bar(ref int x)
    {
        x = 10;
    }
}
