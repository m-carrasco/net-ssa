// RUN: %ilasm /nologo /dll /out:"%T/Test.dll" "%s" 
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Bar(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: IL_0000: s0 = a0
// CHECK: IL_0001: s1 = dup [s0]
// CHECK: IL_0002: s0 = s0 + s1
// CHECK: IL_0003: ret s0
// CHECK: IL_0004: leave.s IL_0007
// CHECK: IL_0006: endfinally
// CHECK: IL_0007: s0 = a0
// CHECK: IL_0008: ret s0

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

	.method public static int32 Bar (int32 a) cil managed
	{
        .maxstack 2
        .try
        {
          // sequence point: hidden
          ldarg.0
          dup
          add
          ret
          IL_007b: leave.s IL_0084
        } // end .try
        fault
        {
          endfinally
        } // end handler

        IL_0084: ldarg.0
        ret
	}
}