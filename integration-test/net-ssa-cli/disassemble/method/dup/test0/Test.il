// RUN: %ilasm /nologo /dll /out:"%T/Test.dll" "%s"
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Bar(System.Int32)" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: s0 = ldarg.0 [a0]
// CHECK: {{.*}}: s1 = dup [s0]
// CHECK: {{.*}}: s0 = add [s0, s1]
// CHECK: {{.*}}: ret [s0]

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
        ldarg.0
        dup
        add
        ret
	}
}
