# net-ssa [![.NET (build, test and release if necessary)](https://github.com/m-carrasco/net-ssa/actions/workflows/build.yml/badge.svg?branch=main&event=push)](https://github.com/m-carrasco/net-ssa/actions/workflows/build.yml) [![Docker image](https://github.com/m-carrasco/net-ssa/actions/workflows/docker-ubuntu.yml/badge.svg?branch=main&event=push)](https://github.com/m-carrasco/net-ssa/actions/workflows/docker-ubuntu.yml) ![Nuget](https://img.shields.io/nuget/v/net-ssa-lib)

Microsoft's high-level programming languages, such as C#, are compiled to CIL bytecode. The instruction set architecture of CIL operates on a stack virtual machine with local variables. CIL instruction's operands are implicit because they are elements in the stack. `net-ssa` provides a register-based intermediate representation for CIL where operands become explicit.

Using CIL properties, it is possible to know for every instruction which slots of the stack it consumes. Similarly, it is possible to know how many elements it pushes into the stack. `net-ssa` computes its initial representation promoting stack slots into registers. In this form, a stack slot promoted to register can be defined more than once. Local variables are accessed through store and load instructions (like LLVM-IR).

The initial register-based representation can be transformed into SSA form. SSA guarantees that every register is only defined once, and its unique definition dominates its uses. This transformation is based on dominance frontiers and partially implemented in Datalog.

`net-ssa` can be either used as a library `net-ssa-lib` or as command-line application `net-ssa-cli`.

If you have any questions or suggestions, feel free to open an issue to discuss it.

### Table of Contents

* [Quick setup](#quick-setup)
* [Build from sources](#build-from-sources)
* [Example: net-ssa-cli](#disassembling-with-net-ssa-cli)
* [Example: net-ssa-lib](#disassembling-with-net-ssa-lib)
* [Contributing](#contributing)
* [Acknowledgements](#acknowledgements)

## Quick setup

It is possible to develop and test `net-ssa` without installing any dependency in your system but [Docker](https://docs.docker.com/get-docker/).
However, it is adviced to compile the project at least once in the host system. This is mainly for downloading dependencies and correctly setting up any IDE.

1. `git clone git@github.com:m-carrasco/net-ssa.git`
2. `cd net-ssa`
3.  `dotnet build && dotnet test`
    * This is optional, it requires installing dotnet.
4. `docker build -t net-ssa/net-ssa .`
5. `docker run --name dev -v $(pwd):/net-ssa -ti net-ssa/net-ssa`
   * This is now an interactive container. `$(pwd)` of the host is shared with the container as `net-ssa` source code folder.
6. Introduce changes in the source code using your IDE as usual.
7. Build and test in the container, execute these commands in the container terminal:
   * `cd build`
   * `(cd /net-ssa && dotnet build)`
   * `lit integration-test/ -vvv`
   * `exit # once you finish working`
8.  `docker start -i dev # to resume the container terminal`


## Build from sources

### Ubuntu 20.04

1. `cd net-ssa`
2. `dotnet build`
3. `dotnet test`
4. `mkdir build`
5. `cd build && cmake ..`
6. `lit integration-test/ -vvv`

To know the required dependencies for the integration tests (`cmake` and `lit` step), please check the [Dockerfile](https://github.com/m-carrasco/net-ssa/blob/main/Dockerfile).
The Dockerfile executes the shell scripts under the [ci](https://github.com/m-carrasco/net-ssa/tree/main/ci) folder. You would just need to execute them once in your system.

### Windows and MacOS

The steps are the same as in Ubuntu. The project building and unit testing is done in the CI. Yet, the integration tests aren't configured.
Anyway, the dependencies should be the same as in Ubuntu. If you encounter any problem while trying this, please open an issue.

## Build native dependencies 

`net-ssa` has native dependencies, which are shipped in the project already. You shouldn't need to build them. Usually, this is only required for development. The supported systems are:
* Linux - x86-64
* Windows - x86-64
* MacOS - x86-64 and arm64

In case they must be re-built, [`./net-ssa/souffle/build-all-with-docker.sh`](https://github.com/m-carrasco/net-ssa/blob/main/souffle/build-all-with-docker.sh) is available. The script compiles these dependencies from scratch using the source code in your repository. Under the hood, the script isolates this process using Docker. This only builds the Linux and Windows dependencies. Cross-compilation for MacOS is incredible difficult. If you are a MacOS user, check the CI to figure out the required dependencies and execute `build-souffle-macos-x86-64-arm64.sh`.

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


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Please make sure to update tests as appropriate.

## Acknowledgements

* Mono.Cecil's max-stack size calculation algorithm.
* Souffle's dominance frontier implementation taken from its test suite.

## License
[MIT](https://choosealicense.com/licenses/mit/)

