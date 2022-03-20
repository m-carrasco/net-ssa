// RUN: %ilasm /nologo /dll /out:"%T/Test.dll" "%s"
// RUN:  %net-ssa-cli %T/Test.dll disassemble --type Ssa all > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: s0_0 = ldarg.0 [a0]
// CHECK: {{.*}}: brfalse.s [[L_0007:.*]] [s0_0]
// CHECK: [[L_0004:.*]]: label
// CHECK: {{.*}}: s0_2 = ldc.i4.1
// CHECK: {{.*}}: br.s L_0009
// CHECK: [[L_0007]]: label
// CHECK: {{.*}}: s0_1 = ldc.i4.2
// CHECK: {{.*}}: label
// CHECK: {{.*}}: s0_3 = phi [(s0_1,[[L_0007]]),(s0_2,[[L_0004]])]
// CHECK: {{.*}}: ret  [s0_3]

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 2:0:0:0
}

.assembly Test { }

.module Test.dll

.class private auto ansi Test
{
	.method public specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [mscorlib]
        System.Object::.ctor ()
        ret
    }

  .method public hidebysig static
    int32 Bar (bool a) cil managed
  {
        .maxstack 1

        IL_0000: ldarg.0
        IL_0001: brfalse.s IL_0007

        IL_0003: ldc.i4.1
        IL_0005: br.s IL_0009

        IL_0007: ldc.i4.2

        IL_0009: ret
  }

}
