// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll list types > %t.types
// RUN: %FileCheck %s < %t.types

// CHECK: Test

public class Test { }
