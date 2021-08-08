// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll list types > %t.types
// RUN: %FileCheck %s < %t.types

// CHECK: MyNamespace.Test
// CHECK: MyNamespace.Test/Nested

namespace MyNamespace
{
    public class Test
    {
        public class Nested { }
    }
}

