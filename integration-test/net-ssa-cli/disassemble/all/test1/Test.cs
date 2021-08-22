// RUN: %net-ssa-cli /usr/lib/mono/4.5/mscorlib.dll disassemble all > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

/*
COM: It is testing only the last method.
CHECK: System.UInt32 <PrivateImplementationDetails>::ComputeStringHash(System.String)
CHECK: 	IL_0000: s0 = a0
CHECK: 	IL_0001: brfalse.s IL_002a [s0]
CHECK: 	IL_0003: s0 = ldc.i4 -2128831035
CHECK: 	IL_0008: l0 = s0
CHECK: 	IL_0009: s0 = 0
CHECK: 	IL_000a: l1 = s0
CHECK: 	IL_000b: br.s IL_0021
CHECK: 	IL_000d: s0 = a0
CHECK: 	IL_000e: s1 = l1
CHECK: 	IL_000f: s0 = callvirt System.Char System.String::get_Chars(System.Int32) [s0, s1]
CHECK: 	IL_0014: s1 = l0
CHECK: 	IL_0015: s0 = xor [s0, s1]
CHECK: 	IL_0016: s1 = ldc.i4 16777619
CHECK: 	IL_001b: s0 = s0 * s1
CHECK: 	IL_001c: l0 = s0
CHECK: 	IL_001d: s0 = l1
CHECK: 	IL_001e: s1 = 1
CHECK: 	IL_001f: s0 = s0 + s1
CHECK: 	IL_0020: l1 = s0
CHECK: 	IL_0021: s0 = l1
CHECK: 	IL_0022: s1 = a0
CHECK: 	IL_0023: s1 = callvirt System.Int32 System.String::get_Length() [s1]
CHECK: 	IL_0028: blt.s IL_000d [s0, s1]
CHECK: 	IL_002a: s0 = l0
CHECK: 	IL_002b: ret s0
*/
