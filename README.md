# net-ssa [![.NET (build, test and release if necessary)](https://github.com/m-carrasco/net-ssa/actions/workflows/build.yml/badge.svg?branch=main&event=push)](https://github.com/m-carrasco/net-ssa/actions/workflows/build.yml) [![Docker image](https://github.com/m-carrasco/net-ssa/actions/workflows/docker-ubuntu.yml/badge.svg?branch=main&event=push)](https://github.com/m-carrasco/net-ssa/actions/workflows/docker-ubuntu.yml) ![Nuget](https://img.shields.io/nuget/v/net-ssa-lib)


Microsoft's high-level programming languages, such as C#, are compiled into Common Intermediate Language (CIL) bytecode. The CIL instruction set operates on a stack virtual machine with implicit operands, as they are elements within the stack. `net-ssa` introduces a register-based intermediate representation for CIL, making operands explicit.

CIL properties allow `net-ssa` to determine the stack slots consumed and the pushed elements amount for each instruction. The initial representation involves promoting stack slots into registers using this information. In this stage, a stack slot promoted to a register may have multiple definitions. Local variables are accessed through store and load instructions, similar to LLVM-IR.

The initial register-based representation can undergo transformation into Static Single Assignment (SSA) form. SSA ensures that each register is defined only once, and its unique definition dominates its uses. This transformation relies on dominance frontiers and is partially implemented in Datalog.

`net-ssa` can be employed either as a library (`net-ssa-lib`) or as a command-line application (`net-ssa-cli`).

Feel free to open an issue to discuss any questions or suggestions you may have.

### Table of Contents

- [net-ssa   ](#net-ssa---)
    - [Table of Contents](#table-of-contents)
  - [Mirror](#mirror)
  - [Quick setup](#quick-setup)
  - [Build from sources](#build-from-sources)
    - [Ubuntu 22.04](#ubuntu-2204)
    - [Windows and macOS](#windows-and-macos)
  - [Build native dependencies](#build-native-dependencies)
  - [Examples](#examples)
    - [Disassembling with net-ssa-cli](#disassembling-with-net-ssa-cli)
    - [Disassembling with net-ssa-lib](#disassembling-with-net-ssa-lib)
  - [Type inference analysis](#type-inference-analysis)
    - [Simple type inference analysis](#simple-type-inference-analysis)
    - [Precise type inference analysis](#precise-type-inference-analysis)
  - [Contributing](#contributing)
  - [Acknowledgements](#acknowledgements)
  - [License](#license)

## Mirror

If you face issues while cloning related to Git LFS, please use the [mirror repository](https://bitbucket.org/m-carrasco/net-ssa-mirror/).
This is caused by Github's Git LFS bandwidth, which is 1GB per month.

## Quick setup

It is possible to develop and test `net-ssa` without installing any dependency in your system but [Docker](https://docs.docker.com/get-docker/).

1. `git clone git@github.com:m-carrasco/net-ssa.git`
2. `cd net-ssa`
3. `git lfs checkout`
    * Install [git lfs](https://git-lfs.github.com/)
4. `./ci/build-image.sh`
5. `./ci/tmp-container.sh`
   * This is now an interactive and temporary container. 
   * The host's folder containing the repository is shared with the container. In the container, this is located at `/home/ubuntu/net-ssa/`.
6. Introduce changes in the source code using your IDE as usual.
7. Build and test in the container, execute these commands in the container terminal:
   * `dotnet build`
   * `dotnet test` 
   * `lit integration-test/ -vvv`

## Build from sources

### Ubuntu 22.04

1. `cd net-ssa`
2. `git lfs checkout`
    * Install [git lfs](https://git-lfs.github.com/)
3. `dotnet build`
4. `dotnet test`
5. `lit integration-test/ -vvv`

Please check the [Dockerfile](https://github.com/m-carrasco/net-ssa/blob/main/Dockerfile) to know which dependencies must be installed.
The Dockerfile executes the shell scripts under the [ci](https://github.com/m-carrasco/net-ssa/tree/main/ci) folder. You would just need to execute them once in your system.

### Windows and macOS

The steps are the same as in Ubuntu. The project building and unit testing is done in the CI. Yet, the integration tests aren't configured.
Anyway, the dependencies should be the same as in Ubuntu. If you encounter any problem while trying this, please open an issue.

## Build native dependencies 

`net-ssa` has native dependencies, which are shipped in the project already. You shouldn't need to build them. Usually, this is only required for development. The supported systems are:
* Linux - x86-64
* Windows - x86-64
* macOS - x86-64 and arm64

In case they must be re-built, [`./net-ssa/souffle/build-all-with-docker.sh`](https://github.com/m-carrasco/net-ssa/blob/main/souffle/build-all-with-docker.sh) is available. The script compiles these dependencies from scratch using the source code in your repository. Under the hood, the script isolates this process using Docker. This only builds the Linux and Windows dependencies. Cross-compilation for macOS is incredible difficult. If you are a macOS user, check the CI to figure out the required dependencies and execute `build-souffle-macos-x86-64-arm64.sh`.

## Examples

### Disassembling with net-ssa-cli

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
4. Use ```net-ssa-cli``` to build a register-based representation of the recently built *dll*: ```net-ssa-cli Example.dll disassemble all```
```
System.UInt32 Example::Factorial(System.UInt32)
L_0000: label
L_0001: nop
L_0002: s0 = ldc.i4.1
L_0003: l0 = stloc.0 [s0]
L_0004: s0 = ldc.i4.1
L_0005: l1 = stloc.1 [s0]
L_0006: br L_0010
L_0007: label
L_0008: s0 = ldloc.0 [l0]
L_0009: s1 = ldloc.1 [l1]
L_000a: s0 = mul [s0, s1]
L_000b: l0 = stloc.0 [s0]
L_000c: s0 = ldloc.1 [l1]
L_000d: s1 = ldc.i4.1
L_000e: s0 = add [s0, s1]
L_000f: l1 = stloc.1 [s0]
L_0010: label
L_0011: s0 = ldloc.1 [l1]
L_0012: s1 = ldarg.0 [a0]
L_0013: ble.un L_0007 [s0, s1]
L_0014: label
L_0015: s0 = ldloc.0 [l0]
L_0016: ret  [s0]
```
5. The previous representation is not in SSA form. Registers are defined more than once. Use ```net-ssa-cli``` to build a register-based representation in SSA form: ```net-ssa-cli Example.dll disassemble --type Ssa all```
```
System.UInt32 Example::Factorial(System.UInt32)
L_0000: label
L_0001: nop
L_0002: s0_0 = ldc.i4.1
L_0003: l0 = stloc.0 [s0_0]
L_0004: s0_1 = ldc.i4.1
L_0005: l1 = stloc.1 [s0_1]
L_0006: br L_0010
L_0007: label
L_0008: s0_4 = ldloc.0 [l0]
L_0009: s1_2 = ldloc.1 [l1]
L_000a: s0_5 = mul [s0_4, s1_2]
L_000b: l0 = stloc.0 [s0_5]
L_000c: s0_6 = ldloc.1 [l1]
L_000d: s1_3 = ldc.i4.1
L_000e: s0_7 = add [s0_6, s1_3]
L_000f: l1 = stloc.1 [s0_7]
L_0010: label
L_0011: s0_3 = ldloc.1 [l1]
L_0012: s1_1 = ldarg.0 [a0]
L_0013: ble.un L_0007 [s0_3, s1_1]
L_0014: label
L_0015: s0_8 = ldloc.0 [l0]
L_0016: ret  [s0_8]
```
6. If SSA is enabled, it is possible to compute a basic type inference for registers:  ```net-ssa-cli Example.dll disassemble --type Ssa --type-inference all```
```
System.UInt32 Example::Factorial(System.UInt32)
LocalVariable l0 ; System.UInt32
LocalVariable l1 ; System.UInt32
ArgumentVariable a0 ; System.UInt32
L_0000: label
L_0001: nop
L_0002: s0_0 = ldc.i4.1 ; Int32
L_0003: l0 = stloc.0 [s0_0]
L_0004: s0_1 = ldc.i4.1 ; Int32
L_0005: l1 = stloc.1 [s0_1]
L_0006: br L_0010
L_0007: label
L_0008: s0_4 = ldloc.0 [l0] ; Int32
L_0009: s1_2 = ldloc.1 [l1] ; Int32
L_000a: s0_5 = mul [s0_4, s1_2] ; Int32
L_000b: l0 = stloc.0 [s0_5]
L_000c: s0_6 = ldloc.1 [l1] ; Int32
L_000d: s1_3 = ldc.i4.1 ; Int32
L_000e: s0_7 = add [s0_6, s1_3] ; Int32
L_000f: l1 = stloc.1 [s0_7]
L_0010: label
L_0011: s0_3 = ldloc.1 [l1] ; Int32
L_0012: s1_1 = ldarg.0 [a0] ; Int32
L_0013: ble.un L_0007 [s0_3, s1_1]
L_0014: label
L_0015: s0_8 = ldloc.0 [l0] ; Int32
L_0016: ret  [s0_8]
```

### Disassembling with net-ssa-lib

```CSharp
// Read Mono.Cecil documentation in order to learn how to load a MethodDefinition from an assembly.
public void YourFunction(Mono.Cecil.MethodDefinition methodDefinition)
{
    Mono.Cecil.Cil.MethodBody body = methodDefinition.Body;

    IRBody irBody = Unstacker.Compute(body);
    // This call is optional.
    Ssa.Compute(irBody);
    // This analysis is optional and it requires SSA
    StackTypeInference analysis = new StackTypeInference(irBody);
    IDictionary<Register, StackType> stackTypes = analysis.Type();

    foreach (NetSsa.Instructions.TacInstruction ins in irBody.Instructions)
    {
        Console.WriteLine(ins);
        if (ins.Result is Register register){
            Console.WriteLine(stackTypes[register]);
        }
    }
}
```

This example is based on the `TestExampleDisassemble` unit test. `net-ssa-cli` can be a good starting point to understand how to call `net-ssa-lib`. 

## Type inference analysis

In SSA, `net-ssa` provides a type inference analysis for registers. Registers come from stack location definitions in the original CIL code (stack-based representation). A register (originally a stack location), according to ECMA-CIL, can hold a value of the following types

* `Int32` and `Int64`
* `NativeInt` (`IntPtr` in C#)
    * The internal representation that the runtime implementation chooses for pointers. Nowadays, it usually is `Int64`. 
* `NativeFloat`
    * The internal representation that the runtime implementation chooses for `float32` and `float64`. Usually, it is `float64`.
* `ObjectReference`
    * An assembly-defined class.
    * A boxed value (the `Box` class is not defined in any assembly).
    * A null value (the `Null` class loaded by `ldnull` is not defined in any assembly).
* `UserDefinedValueType`
    * Value types which are not the built-in ones.
* `ManagedPointer`
* `GenericParameter`

`net-ssa` provides an analysis to infer this information for each register. This analysis provides two operation modes in regard to the precision of the results. The analysis is implemented in the `StackTypeInference` class.

### Simple type inference analysis

The simple type inference just characterizes the kind of value that a register can hold. The infered `StackType` for a register can be

* `Int32` and `Int64`
* `NativeInt`
* `NativeFloat`
* `UserDefinedValueType`
    * with a reference to the actual user-defined value type.
    * ECMA-CIL does not allow merge points of value types, so it is possible to precisely compute it.
* `ManagedPointer`
    * with a reference to the actual managed pointer type (if possible).
    * Merging managed pointers is legal but not verifiable CIL. To the best of my knowledge, there is no hierarchy of managed pointers. 
* `GenericParameter`
    * with a reference to the actual generic parameter (if possible).
* `ObjectReference`
    * with a reference to the actual class (as long as it doesn't involve unsafe memory accesses or merging different types).
    * Merging implies reasoning about the class hierarchy which is not built-in in `Mono.Cecil`. This involves `phi` instructions.  

The simple operation mode can be called as shown in the `TestExampleDisassemble` unit test. In `net-ssa-cli`, the basic mode can be enabled using `-type-inference=basic` (SSA only).

### Precise type inference analysis

The precise operation can provide the actual class that an object reference is. This is true as long as it does not involve unsafe load operations or the `Refanytype` opcode. The infered `StackType` for a register can be

* `Int32` and `Int64`
* `NativeInt`
* `NativeFloat`
* `UserDefinedValueType`
    * with a reference to the actual user-defined value type.
    * ECMA-CIL does not allow merge points of value types, so it is possible to precisely return it.
* `ManagedPointer`
    * with a reference to the actual managed pointer type (if possible).
    * Merging managed pointers is legal but not verifiable CIL. To the best of my knowledge, there is no hierarchy of managed pointers. 
* `GenericParameter`
    * with a reference to the actual generic parameter (if possible).
* `ObjectReference`
    * with a reference to the actual class (if possible). This works also for non-assembly types.
    * Unsafe memory accesess or opcodes such as `Refanytype` are not handled. In these cases, the analysis just infers that it is an `ObjectReference` but not the actual class.

The precise operation mode can be called as shown in the `MergeTypeMscorlib` unit test. The class hierarchy analysis converts `Mono.Cecil.TypeReference` to `System.Type`. This simplifies the process of writing from scratch subtying rules, etc. `System.Type` references are converted back to `Mono.Cecil.TypeReference`. In `net-ssa-cli`, the precise mode can be enabled using `-type-inference=precise` (SSA only).

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## Acknowledgements

* Mono.Cecil's max-stack size calculation algorithm.
* Souffle's dominance frontier implementation taken from its test suite.

## License
[MIT](https://choosealicense.com/licenses/mit/)

