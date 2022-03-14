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
    class PrintCfg
    {
        enum DisassemblyType
        {
            IR,
            Ssa
        }

        public static void AddControlFlowGraphSubCommand(RootCommand rootCommand)
        {
            var cfg = new Command("cfg");

            var type = new Option<DisassemblyType>(
                "--type",
                getDefaultValue: () => DisassemblyType.IR,
                description: "Type of the dissasmbled code.");
            cfg.AddOption(type);

            var method = new Command("method");
            method.AddArgument(new Argument<String>("method", "Method to print CFG."));
            method.Handler = CommandHandler.Create<FileInfo, DisassemblyType, String>(Print);
            cfg.AddCommand(method);

            rootCommand.Add(cfg);
        }

        static void Print(FileInfo input, DisassemblyType type, String method)
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

                            if (DisassemblyType.Ssa.Equals(type))
                            {
                                Ssa.Compute(m, irBody);
                            }
                            ControlFlowGraph cfg = new ControlFlowGraph(irBody);

                            Console.WriteLine(cfg.SerializeDotFile());
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

                        IRBody irBody = Unstacker.Compute(m.Body);

                        bool isSsa = DisassemblyType.Ssa.Equals(type);
                        if (isSsa)
                        {
                            Ssa.Compute(m, irBody);

                        }

                        Console.WriteLine(String.Join(System.Environment.NewLine, irBody.Instructions.Select(t => t.ToString())));

                        if (isSsa)
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
