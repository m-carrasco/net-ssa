// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll list methods Test/Nested > %t.methods
// RUN: %FileCheck %s < %t.methods

// CHECK: System.Void Test/Nested::.ctor()

public class Test
{
    public class Nested { }
}
