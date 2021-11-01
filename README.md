
# net-ssa  [![.NET](https://github.com/m-carrasco/net-ssa/actions/workflows/build.yml/badge.svg)](https://github.com/m-carrasco/net-ssa/actions/workflows/build.yml)

`net-ssa-lib` is a library that provides a register-based representation for .NET bytecode. In its simplest form, each stack and local variable slot is promoted to a unique register.  Additionally, it can be transformed into SSA (Static single assignment) form using [Souffle](https://souffle-lang.github.io/), a Datalog engine. In addition, typing information is available for each register. 
 
Additionally, `net-ssa-cli` is a CLI application that works as a driver of the library’s main features. For instance, given a .NET *dll* or *exe*, it can disassemble it.

## Installation

### Ubuntu 20.04.3

1. Read Dockerfile
  
### Ubuntu 18.04

1. Read ./github/workflow/build.yml

### Windows

It has not been tested yet. However, it should work as long as Souffle can be installed. Souffle can run there using _Windows Subsystem for Linux_.

## Usage: Disassembling with net-ssa-cli

1. Build the following code snippet:  ```mcs -target:library Example.cs```. ```msc``` is used but any C# compiler can be used.

```CSharp
public class Example{
    public static uint Factorial(uint number)
    {
        uint accumulator = 1;
        for (uint factor = 1; factor <= number; factor++)
        {
            accumulator *= factor;
        }
        return accumulator;
    }
 }
```
2. To compare, disassemble the original bytecode which is in a stack-based representation: ```monodis Example.dll```

```
    .method public static
           unsigned int32 Factorial (unsigned int32 number)  cil managed 
    {
        .maxstack 2
        .locals init (
                unsigned int32  V_0,
                unsigned int32  V_1)
        IL_0000:  ldc.i4.1 
        IL_0001:  stloc.0 
        IL_0002:  ldc.i4.1 
        IL_0003:  stloc.1 
        IL_0004:  br IL_0011

        IL_0009:  ldloc.0 
        IL_000a:  ldloc.1 
        IL_000b:  mul 
        IL_000c:  stloc.0 
        IL_000d:  ldloc.1 
        IL_000e:  ldc.i4.1 
        IL_000f:  add 
        IL_0010:  stloc.1 
        IL_0011:  ldloc.1 
        IL_0012:  ldarg.0 
        IL_0013:  ble.un IL_0009

        IL_0018:  ldloc.0 
        IL_0019:  ret 
    }
```
4. Finally, use ```net-ssa-cli``` to build a register-based representation of the recently built *dll*: ```net-ssa-cli Example.dll disassemble all``` 
```
System.UInt32 Example::Factorial(System.UInt32)
        IL_0000: s0 = 1
        IL_0001: l0 = s0
        IL_0002: s0 = 1
        IL_0003: l1 = s0
        IL_0004: br IL_0011
        
        IL_0009: s0 = l0
        IL_000a: s1 = l1
        IL_000b: s0 = s0 * s1
        IL_000c: l0 = s0
        IL_000d: s0 = l1
        IL_000e: s1 = 1
        IL_000f: s0 = s0 + s1
        IL_0010: l1 = s0
        IL_0011: s0 = l1
        IL_0012: s1 = a0
        IL_0013: br IL_0009 if s0 <= s1 [unsigned]
        
        IL_0018: s0 = l0
        IL_0019: ret s0
```
## Usage: Disassembling with net-ssa-lib

```CSharp
// Read Mono.Cecil documentation in order to learn how to load a MethodDefinition from an assembly.
public void YourFunction(Mono.Cecil.MethodDefinition methodDefinition)
{
    Mono.Cecil.MethodDefinition methodDefinition = definedMethods.Where(method => method.Name.Contains("TestPhiCode")).Single();
    Mono.Cecil.Cil.MethodBody body = methodDefinition.Body;

    NetSsa.Analyses.BytecodeBody bytecodeBody = Bytecode.Compute(body);

    foreach (NetSsa.Instructions.BytecodeInstruction ins in bytecodeBody.Instructions)
    {
        Mono.Cecil.Cil.Instruction cil = ins.Bytecode;
        Console.WriteLine("Opcode: " + cil.OpCode.Code);

        foreach (NetSsa.Analyses.Variable op in ins.Operands)
        {
            Console.WriteLine("Operand: " + op.Name);
        }

        if (ins.Result != null)
        {
            Console.WriteLine("Result: " + ins.Result.Name);
        }
    }
}
```
## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License
[MIT](https://choosealicense.com/licenses/mit/)

