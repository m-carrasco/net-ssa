// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll list methods Test > %t.methods
// RUN: %FileCheck %s < %t.methods

// CHECK: System.Void Test::.ctor()
// CHECK: System.Void Test::foo()

public class Test
{
    public static void foo() { }
}
