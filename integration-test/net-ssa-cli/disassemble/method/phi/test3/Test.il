// RUN: %ilasm /nologo /dll /out:"%T/Test.dll" "%s" 
// RUN:  %net-ssa-cli %T/Test.dll disassemble --type Ssa all > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: 	IL_0000: s0_0 = a0 [load]
// CHECK: 	IL_0001: brfalse.s IL_0006 [s0_0]

// CHECK: 	IL_0003: s0_2 = 1
// CHECK: 	IL_0004: br.s IL_0007

// CHECK: 	IL_0006: s0_1 = 2

// CHECK: 	PHI_0000: s0_3 = phi [(s0_2,IL_0004),(s0_1,IL_0006)]
// CHECK: 	IL_0007: ret s0_3

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