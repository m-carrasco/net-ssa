using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetSsa.Analyses;
using NetSsa.Instructions;

namespace NetSsaCli
{
    class Disassemble
    {
        enum DisassemblyType
        {
            Bytecode,
            IR,
            Ssa
        }

        public static void AddDisassasembleSubCommand(RootCommand rootCommand)
        {
            var disassemble = new Command("disassemble");

            var type = new Option<DisassemblyType>(
                "--type",
                getDefaultValue: () => DisassemblyType.Bytecode,
                description: "Type of the dissasmbled code.");
            disassemble.AddOption(type);

            var method = new Command("method");
            method.AddArgument(new Argument<String>("method", "Method to disassemble."));
            method.Handler = CommandHandler.Create<FileInfo, DisassemblyType, String>(PrintDisassemble);
            disassemble.AddCommand(method);

            var all = new Command("all", "All methods in the assembly are disassembled.");
            all.Handler = CommandHandler.Create<FileInfo, DisassemblyType>(PrintDisassemble);
            disassemble.AddCommand(all);

            rootCommand.Add(disassemble);
        }

        static void PrintDisassemble(FileInfo input, DisassemblyType type, String method)
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input.FullName))
            {
                foreach (TypeDefinition t in assembly.MainModule.GetTypes())
                {
                    foreach (MethodDefinition m in t.Methods)
                    {
                        if (m.FullName.Equals(method))
                        {
                            if (!m.HasBody)
                            {
                                Console.WriteLine("Method has no body.");
                                return;
                            }

                            BytecodeBody bytecodeBody = Bytecode.Compute(m.Body);

                            if (DisassemblyType.IR.Equals(type) || DisassemblyType.Ssa.Equals(type))
                            {
                                IR.VariableDefinitionsToUses(bytecodeBody);
                            }

                            if (DisassemblyType.Ssa.Equals(type))
                            {
                                SsaBody ssaBody = Ssa.Compute(m, bytecodeBody);
                                Console.WriteLine(String.Join(System.Environment.NewLine, ssaBody.Instructions.Select(t => "\t" + t.ToString())));
                                return;
                            }

                            Console.WriteLine(String.Join(System.Environment.NewLine, bytecodeBody.Instructions.Select(t => t.ToString())));
                            return;
                        }
                    }
                }

                Console.WriteLine("No method found.");
            }
        }

        static void PrintDisassemble(FileInfo input, DisassemblyType type)
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input.FullName))
            {
                foreach (TypeDefinition t in assembly.MainModule.GetTypes())
                {
                    foreach (MethodDefinition m in t.Methods)
                    {
                        Console.WriteLine(m.FullName);
                        if (!m.HasBody)
                        {
                            Console.WriteLine("\tMethod has no body.");
                            continue;
                        }

                        BytecodeBody bytecodeBody = Bytecode.Compute(m.Body);

                        if (DisassemblyType.IR.Equals(type) || DisassemblyType.Ssa.Equals(type))
                        {
                            IR.VariableDefinitionsToUses(bytecodeBody);
                        }

                        if (DisassemblyType.Ssa.Equals(type))
                        {
                            SsaBody ssaBody = Ssa.Compute(m, bytecodeBody);
                            Console.WriteLine(String.Join(System.Environment.NewLine, ssaBody.Instructions.Select(t => "\t" + t.ToString())));
                            continue;
                        }

                        Console.WriteLine(String.Join(System.Environment.NewLine, bytecodeBody.Instructions.Select(t => t.ToString())));
                    }
                }
            }
        }

    }
}
