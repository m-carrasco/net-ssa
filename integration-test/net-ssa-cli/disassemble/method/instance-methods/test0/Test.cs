// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Foo(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: call System.Void System.Console::WriteLine(System.Object) [s0]
// CHECK: {{.*}}: s0 = ldarg.1 [a1]
// CHECK: {{.*}}: ret [s0]

using System;

public class Test
{
    public int Foo(int a)
    {
        Console.WriteLine(this);
        return a;
    }
}
