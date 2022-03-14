// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Void Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.}}: s0 = ldarga.s a [a0]
// CHECK: {{.}}: l0 = stloc.0 [s0]
// CHECK: {{.}}: s0 = ldloc.0 [l0]
// CHECK: {{.}}: call System.Void Test::Bar(System.Int32&) [s0]
// CHECK: {{.}}: ret

using System;

public class Test
{
    public static void Foo(int a)
    {
        ref int addr = ref a;
        Bar(ref addr);
    }

    public static void Bar(ref int x)
    {
        x = 10;
    }
}
