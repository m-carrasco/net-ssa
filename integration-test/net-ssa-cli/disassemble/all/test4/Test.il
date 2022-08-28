// RUN: %ilasm /nologo /dll /out:"%T/Test.dll" "%s"
// RUN: %net-ssa-cli %T/Test.dll disassemble --type-inference precise --type Ssa all > %t.disassemble
// RUN: %FileCheck %s < %t.disassemble

// COM: The two exceptions are merged into System.SystemException.
// COM: That is the least common ancestor of the two types.
// CHECK: System.Void C::merge(System.Boolean)
// CHECK: ArgumentVariable a0 ; C
// CHECK: ArgumentVariable a1 ; System.Boolean
// CHECK: {{.*}}: label
// CHECK: {{.*}}: nop
// CHECK: {{.*}}: s0_0 = ldarg.1 [a1] ; Int32
// CHECK: {{.*}}: brtrue.s [[L_0007:.*]] [s0_0]
// CHECK: [[L_0004:.*]]: label
// CHECK: {{.*}}: s0_2 = newobj System.Void System.NotImplementedException::.ctor() ; ObjectRef<System.NotImplementedException>
// CHECK: {{.*}}: br.s [[L_0009:.*]]
// CHECK: [[L_0007]]: label
// CHECK: {{.*}}: s0_1 = newobj System.Void System.NotSupportedException::.ctor() ; ObjectRef<System.NotSupportedException>
// CHECK: [[L_0009]]: label
// CHECK: {{.*}}: s0_3 = phi [(s0_1,[[L_0007]]),(s0_2,[[L_0004:.*]])] ; ObjectRef<System.SystemException>
// CHECK: {{.*}}: throw  [s0_3]

.assembly extern System.Runtime
{
  .ver 6:0:0:0
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) // .?_....:
}
.assembly 'bla'
{
  .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::'.ctor'(int32) =  (01 00 08 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::'.ctor'() =  (
		01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
		63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01       ) // ceptionThrows.

  .custom instance void class [mscorlib]System.Diagnostics.DebuggableAttribute::'.ctor'(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) =  (01 00 02 00 00 00 00 00 ) // ........

  .custom instance void class [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::'.ctor'(string) =  (
		01 00 18 2E 4E 45 54 43 6F 72 65 41 70 70 2C 56   // ....NETCoreApp,V
		65 72 73 69 6F 6E 3D 76 36 2E 30 01 00 54 0E 14   // ersion=v6.0..T..
		46 72 61 6D 65 77 6F 72 6B 44 69 73 70 6C 61 79   // FrameworkDisplay
		4E 61 6D 65 00                                  ) // Name.

  .custom instance void class [mscorlib]System.Reflection.AssemblyCompanyAttribute::'.ctor'(string) =  (01 00 03 62 6C 61 00 00 ) // ...bla..

  .custom instance void class [mscorlib]System.Reflection.AssemblyConfigurationAttribute::'.ctor'(string) =  (01 00 07 52 65 6C 65 61 73 65 00 00 ) // ...Release..

  .custom instance void class [mscorlib]System.Reflection.AssemblyFileVersionAttribute::'.ctor'(string) =  (01 00 07 31 2E 30 2E 30 2E 30 00 00 ) // ...1.0.0.0..

  .custom instance void class [mscorlib]System.Reflection.AssemblyInformationalVersionAttribute::'.ctor'(string) =  (01 00 05 31 2E 30 2E 30 00 00 ) // ...1.0.0..

  .custom instance void class [mscorlib]System.Reflection.AssemblyProductAttribute::'.ctor'(string) =  (01 00 03 62 6C 61 00 00 ) // ...bla..

  .custom instance void class [mscorlib]System.Reflection.AssemblyTitleAttribute::'.ctor'(string) =  (01 00 03 62 6C 61 00 00 ) // ...bla..

  .hash algorithm 0x00008004
  .ver  1:0:0:0
}
.module bla.dll // GUID = {2AAA3AD9-7A68-42CD-A4B9-8A17583B4D0E}


.namespace Microsoft.CodeAnalysis
{
  .class private auto ansi sealed beforefieldinit EmbeddedAttribute
  	extends [System.Runtime]System.Attribute
  {
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class Microsoft.CodeAnalysis.EmbeddedAttribute::'.ctor'() =  (01 00 00 00 ) // ....


    // method line 1
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x2050
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ret 
    } // end of method EmbeddedAttribute::.ctor

  } // end of class Microsoft.CodeAnalysis.EmbeddedAttribute
}

.namespace System.Runtime.CompilerServices
{
  .class private auto ansi sealed beforefieldinit NullableAttribute
  	extends [System.Runtime]System.Attribute
  {
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class Microsoft.CodeAnalysis.EmbeddedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (
		01 00 84 6B 00 00 02 00 54 02 0D 41 6C 6C 6F 77   // ...k....T..Allow
		4D 75 6C 74 69 70 6C 65 00 54 02 09 49 6E 68 65   // Multiple.T..Inhe
		72 69 74 65 64 00                               ) // rited.

    .field  public initonly  unsigned int8[] NullableFlags

    // method line 2
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int8 A_1)  cil managed 
    {
        // Method begins at RVA 0x2058
	// Code size 23 (0x17)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldc.i4.1 
	IL_0008:  newarr [System.Runtime]System.Byte
	IL_000d:  dup 
	IL_000e:  ldc.i4.0 
	IL_000f:  ldarg.1 
	IL_0010:  stelem.i1 
	IL_0011:  stfld unsigned int8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
	IL_0016:  ret 
    } // end of method NullableAttribute::.ctor

    // method line 3
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int8[] A_1)  cil managed 
    {
        // Method begins at RVA 0x2070
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld unsigned int8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
	IL_000d:  ret 
    } // end of method NullableAttribute::.ctor

  } // end of class System.Runtime.CompilerServices.NullableAttribute
}

.namespace System.Runtime.CompilerServices
{
  .class private auto ansi sealed beforefieldinit NullableContextAttribute
  	extends [System.Runtime]System.Attribute
  {
    .custom instance void class [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class Microsoft.CodeAnalysis.EmbeddedAttribute::'.ctor'() =  (01 00 00 00 ) // ....

    .custom instance void class [mscorlib]System.AttributeUsageAttribute::'.ctor'(valuetype [mscorlib]System.AttributeTargets) =  (
		01 00 4C 14 00 00 02 00 54 02 0D 41 6C 6C 6F 77   // ..L.....T..Allow
		4D 75 6C 74 69 70 6C 65 00 54 02 09 49 6E 68 65   // Multiple.T..Inhe
		72 69 74 65 64 00                               ) // rited.

    .field  public initonly  unsigned int8 Flag

    // method line 4
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' (unsigned int8 A_1)  cil managed 
    {
        // Method begins at RVA 0x207f
	// Code size 14 (0xe)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void class [mscorlib]System.Attribute::'.ctor'()
	IL_0006:  ldarg.0 
	IL_0007:  ldarg.1 
	IL_0008:  stfld unsigned int8 System.Runtime.CompilerServices.NullableContextAttribute::Flag
	IL_000d:  ret 
    } // end of method NullableContextAttribute::.ctor

  } // end of class System.Runtime.CompilerServices.NullableContextAttribute
}

  .class public auto ansi beforefieldinit C
  	extends [System.Runtime]System.Object
  {

    // method line 5
    .method public hidebysig 
           instance default void merge (bool b)  cil managed 
    {
        // Method begins at RVA 0x208e
	// Code size 16 (0x10)
	.maxstack 8
	IL_0000:  ldarg.1 
	IL_0001:  brtrue.s IL_000a

	IL_0003:  newobj instance void class [mscorlib]System.NotImplementedException::'.ctor'()
	IL_0008:  br.s IL_000f

	IL_000a:  newobj instance void class [mscorlib]System.NotSupportedException::'.ctor'()
	IL_000f:  throw 
    } // end of method C::merge

    // method line 6
    .method public static hidebysig 
           default void Main (string[] args)  cil managed 
    {
        .custom instance void class System.Runtime.CompilerServices.NullableContextAttribute::'.ctor'(unsigned int8) =  (01 00 01 00 00 ) // .....

        // Method begins at RVA 0x209f
	.entrypoint
	// Code size 1 (0x1)
	.maxstack 8
	IL_0000:  ret 
    } // end of method C::Main

    // method line 7
    .method public hidebysig specialname rtspecialname 
           instance default void '.ctor' ()  cil managed 
    {
        // Method begins at RVA 0x20a1
	// Code size 7 (0x7)
	.maxstack 8
	IL_0000:  ldarg.0 
	IL_0001:  call instance void object::'.ctor'()
	IL_0006:  ret 
    } // end of method C::.ctor

  } // end of class C

