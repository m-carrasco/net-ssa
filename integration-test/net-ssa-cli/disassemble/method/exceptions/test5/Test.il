// RUN: %ilasm /nologo /dll /out:"%T/Test.dll" "%s"
// RUN: %net-ssa-cli %T/Test.dll disassemble method "System.Int32 Test::Bar()" > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// CHECK: {{.*}}: s0 = newobj System.Void System.InvalidOperationException::.ctor()
// CHECK: {{.*}}: throw [s0]
// CHECK: {{.*}}: leave.s [[IL_0008:.*]] [e0]
// COM: leave must consume everything on the stack.
// COM: the stack size after a leave must be 0.
// CHECK: [[IL_0008]]: s0 = ldc.i4.s 42
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

  .method public hidebysig static
    int32 Bar () cil managed
  {
    .maxstack 1

    .try
    {
      IL_0000: newobj    instance void [System.Runtime]System.InvalidOperationException::.ctor()
      IL_0005: throw
    } // end .try
    catch [System.Runtime]System.Exception
    {
      IL_0006: leave.s   IL_0008
    } // end handler

    IL_0008: ldc.i4.s  42
    IL_000A: ret
  }

}
