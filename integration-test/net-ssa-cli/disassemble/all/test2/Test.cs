// RUN:  %net-ssa-cli /usr/lib/mono/4.5/mscorlib.dll disassemble --type Ssa all > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

/*
COM: No phi node should consume an undefined value
CHECK-NOT: (undefined,
CHECK: System.Boolean System.DateTimeParse::AdjustHour(System.Int32&,System.DateTimeParse/TM)
CHECK: IL_0000: s0_0 = a1 [load]
CHECK: IL_0001: s1_0 = -1
CHECK: IL_0002: beq.s IL_003d [s0_0, s1_0]

CHECK: IL_0004: s0_3 = a1 [load]
CHECK: IL_0005: br IL_0023 if s0_3 == true

CHECK: IL_0007: s0_13 = a0 [load]
CHECK: IL_0008: s0_14 = ldind.i4 [s0_13]
CHECK: IL_0009: s1_9 = 0
CHECK: IL_000a: blt.s IL_0012 [s0_14, s1_9]

CHECK: IL_000c: s0_17 = a0 [load]
CHECK: IL_000d: s0_18 = ldind.i4 [s0_17]
CHECK: IL_000e: s1_11 = ldc.i4.s 12
CHECK: IL_0010: ble.s IL_0014 [s0_18, s1_11]

CHECK: IL_0012: s0_16 = 0
CHECK: IL_0013: ret s0_16

CHECK: IL_0014: s0_19 = a0 [load]
CHECK: IL_0015: s1_12 = a0 [load]
CHECK: IL_0016: s1_13 = ldind.i4 [s1_12]
CHECK: IL_0017: s2_2 = ldc.i4.s 12
CHECK: IL_0019: beq.s IL_001f [s1_13, s2_2]

CHECK: IL_001b: s1_15 = a0 [load]
CHECK: IL_001c: s1_16 = ldind.i4 [s1_15]
CHECK: IL_001d: br.s IL_0020

CHECK: IL_001f: s1_14 = 0

CHECK: PHI_0005: s1_17 = phi [(s1_16,IL_001d),(s1_14,IL_001f)]
CHECK: IL_0020: stind.i4 [s0_19, s1_17]
CHECK: IL_0021: br.s IL_003d

CHECK: IL_0023: s0_4 = a0 [load]
CHECK: IL_0024: s0_5 = ldind.i4 [s0_4]
CHECK: IL_0025: s1_2 = 0
CHECK: IL_0026: blt.s IL_002e [s0_5, s1_2]

CHECK: IL_0028: s0_8 = a0 [load]
CHECK: IL_0029: s0_9 = ldind.i4 [s0_8]
CHECK: IL_002a: s1_4 = ldc.i4.s 23
CHECK: IL_002c: ble.s IL_0030 [s0_9, s1_4]

CHECK: IL_002e: s0_7 = 0
CHECK: IL_002f: ret s0_7

CHECK: IL_0030: s0_10 = a0 [load]
CHECK: IL_0031: s0_11 = ldind.i4 [s0_10]
CHECK: IL_0032: s1_5 = ldc.i4.s 12
CHECK: IL_0034: bge.s IL_003d [s0_11, s1_5]

CHECK: IL_0036: s0_12 = a0 [load]
CHECK: IL_0037: s1_6 = a0 [load]
CHECK: IL_0038: s1_7 = ldind.i4 [s1_6]
CHECK: IL_0039: s2_1 = ldc.i4.s 12
CHECK: IL_003b: s1_8 = s1_7 + s2_1
CHECK: IL_003c: stind.i4 [s0_12, s1_8]

CHECK: IL_003d: s0_2 = 1
CHECK: IL_003e: ret s0_2
CHECK-NOT: (undefined,
*/
