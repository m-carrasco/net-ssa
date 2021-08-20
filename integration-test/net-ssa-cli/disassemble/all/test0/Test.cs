// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll disassemble all > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

/*
CHECK: System.Void Bar::.ctor()
CHECK: 	IL_0000: s0 = a0
CHECK: 	IL_0001: call System.Void System.Object::.ctor() [s0]
CHECK: 	IL_0006: ret 
CHECK: System.Int32 Bar::Foo()
CHECK: 	IL_0000: s0 = 0
CHECK: 	IL_0001: ret s0
CHECK: System.Void Baz::.ctor()
CHECK: 	IL_0000: s0 = a0
CHECK: 	IL_0001: call System.Void System.Object::.ctor() [s0]
CHECK: 	IL_0006: ret 
CHECK: System.Int32 Baz::Fuu()
CHECK: 	IL_0000: s0 = 0
CHECK: 	IL_0001: ret s0
*/

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
