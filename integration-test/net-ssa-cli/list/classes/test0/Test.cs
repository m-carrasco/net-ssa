// RUN: %mcs -target:library -out:%T/Test.dll %s
// RUN: %net-ssa-cli %T/Test.dll list classes > %t.classes
// RUN: %FileCheck %s < %t.classes

// CHECK: Test

public class Test { }
