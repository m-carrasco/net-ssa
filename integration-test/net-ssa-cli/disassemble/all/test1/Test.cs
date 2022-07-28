// RUN: %net-ssa-cli %test-resources-dir/mscorlib.dll disassemble all > %t.disassemble
// RUN: %FileCheck %s --match-full-lines < %t.disassemble

// COM: It is testing only the last method.
// CHECK: System.UInt32 <PrivateImplementationDetails>::ComputeStringHash(System.String)
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: brfalse.s [[TARGET0:.*]] [s0]
// CHECK: {{.*}}: label
// CHECK: {{.*}}: s0 = ldc.i4 -2128831035
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldc.i4.0
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: {{.*}}: br.s [[TARGET1:.*]]
// CHECK: [[TARGET2:.*]]: label
// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: s1 = ldloc.1 [l1]
// CHECK: {{.*}}: s0 = callvirt System.Char System.String::get_Chars(System.Int32) [s0, s1]
// CHECK: {{.*}}: s1 = ldloc.0 [l0]
// CHECK: {{.*}}: s0 = xor [s0, s1]
// CHECK: {{.*}}: s1 = ldc.i4 16777619
// CHECK: {{.*}}: s0 = mul [s0, s1]
// CHECK: {{.*}}: l0 = stloc.0 [s0]
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: s1 = ldc.i4.1
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: l1 = stloc.1 [s0]
// CHECK: [[TARGET1]]: label
// CHECK: {{.*}}: s0 = ldloc.1 [l1]
// CHECK: {{.*}}: s1 = ldarg.0 [a0]
// CHECK: {{.*}}: s1 = callvirt System.Int32 System.String::get_Length() [s1]
// CHECK: {{.*}}: blt.s [[TARGET2]] [s0, s1]
// CHECK: [[TARGET0]]: label
// CHECK: {{.*}}: s0 = ldloc.0 [l0]
// CHECK: {{.*}}: ret  [s0]
