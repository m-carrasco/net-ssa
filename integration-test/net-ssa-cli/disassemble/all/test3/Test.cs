// RUN: %net-ssa-cli /usr/lib/mono/4.5/mscorlib.dll disassemble --type=Ssa --type-inference=true all > %t.disassemble
// RUN: %FileCheck %s --match-full-lines < %t.disassemble

// CHECK: System.Void System.IO.TextWriter::Write(System.Boolean)
// CHECK: ArgumentVariable a0 ; System.IO.TextWriter
// CHECK: ArgumentVariable a1 ; System.Boolean
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: s0_0 = ldarg.0 [a0] ; NativeObjectRef
// CHECK: {{.*}}: s1_0 = ldarg.1 [a1] ; Int32
// CHECK: {{.*}}: brtrue.s [[TARGET1:.*]] [s1_0]
// CHECK: [[TARGET2:.*]]: label
// CHECK: {{.*}}: s1_2 = ldstr "False" ; NativeObjectRef
// CHECK: {{.*}}: br.s [[TARGET3:.*]]
// CHECK: [[TARGET1]]: label
// CHECK: {{.*}}: s1_1 = ldstr "True" ; NativeObjectRef
// CHECK: [[TARGET3]]: label
// CHECK: {{.*}}: s1_3 = phi [(s1_1,[[TARGET1]]),(s1_2,[[TARGET2]])] ; NativeObjectRef
// CHECK: {{.*}}: callvirt System.Void System.IO.TextWriter::Write(System.String) [s0_0, s1_3]
// CHECK: {{.*}}: ret