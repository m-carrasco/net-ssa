// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble all > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: System.Void Bar::.ctor()
// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: call System.Void System.Object::.ctor() [s0]
// CHECK: {{.*}}: ret
// CHECK: System.Int32 Bar::Foo()
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: ret [s0]
// CHECK: System.Void Baz::.ctor()
// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: call System.Void System.Object::.ctor() [s0]
// CHECK: {{.*}}: ret
// CHECK: System.Int32 Baz::Fuu()
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: ret [s0]

using System;

public class Bar
{
    public static int Foo()
    {
        return 0;
    }
}

public class Baz
{
    public static int Fuu()
    {
        return 0;
    }
}
