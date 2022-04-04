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
            IR,
            Ssa
        }

        public static void AddDisassasembleSubCommand(RootCommand rootCommand)
        {
            var disassemble = new Command("disassemble");

            var type = new Option<DisassemblyType>(
                "--type",
                getDefaultValue: () => DisassemblyType.IR,
                description: "Type of the dissasmbled code.");
            disassemble.AddOption(type);

            var verifySsa = new Option<bool>(
                "--verify-ssa",
                getDefaultValue: () => false,
                description: "Perform SSA correctness checks (one assignment, etc.).");
            disassemble.AddOption(verifySsa);

            var method = new Command("method");
            method.AddArgument(new Argument<String>("method", "Method to disassemble."));
            method.Handler = CommandHandler.Create<FileInfo, DisassemblyType, bool, String>(PrintDisassemble);
            disassemble.AddCommand(method);

            var all = new Command("all", "All methods in the assembly are disassembled.");
            all.Handler = CommandHandler.Create<FileInfo, DisassemblyType, bool>(PrintDisassemble);
            disassemble.AddCommand(all);

            rootCommand.Add(disassemble);
        }

        static void PrintDisassemble(FileInfo input, DisassemblyType type, bool verifySsa, String method)
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

                            IRBody irBody = Unstacker.Compute(m.Body);

                            bool isSsa = DisassemblyType.Ssa.Equals(type);
                            if (isSsa)
                            {
                                Ssa.Compute(irBody);

                            }

                            Console.WriteLine(m.FullName);
                            Console.WriteLine(String.Join(System.Environment.NewLine, irBody.Instructions.Select(t => t.ToString())));

                            if (isSsa && verifySsa)
                            {
                                SsaVerifier ssaVerifier = new SsaVerifier(irBody);
                                ssaVerifier.Verify();
                            }

                            return;
                        }
                    }
                }

                Console.WriteLine("No method found.");
            }
        }

        static void PrintDisassemble(FileInfo input, DisassemblyType type, bool verifySsa)
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

                        IRBody irBody = Unstacker.Compute(m.Body);

                        bool isSsa = DisassemblyType.Ssa.Equals(type);
                        if (isSsa)
                        {
                            Ssa.Compute(irBody);

                        }

                        Console.WriteLine(String.Join(System.Environment.NewLine, irBody.Instructions.Select(t => t.ToString())));

                        if (isSsa && verifySsa)
                        {
                            SsaVerifier ssaVerifier = new SsaVerifier(irBody);
                            ssaVerifier.Verify();
                        }
                    }
                }
            }
        }

    }
}
