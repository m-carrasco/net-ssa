// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll list methods > %t.methods
// RUN: %FileCheck %s < %t.methods

// CHECK: System.Void MyNamespace.Test`1::.ctor()
// CHECK: T MyNamespace.Test`1::Bar(T,W,MyNamespace.Test`1<W>)


namespace MyNamespace
{
    public class Test<T>
    {
        public static T Bar<T, W>(T t, W w, Test<W> tt) { return t; }
    }
}