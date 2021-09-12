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
        public static void AddDisassasembleSubCommand(RootCommand rootCommand)
        {
            var disassemble = new Command("disassemble");

            var method = new Command("method");
            method.AddArgument(new Argument<String>("method", "Method to disassemble."));
            method.Handler = CommandHandler.Create<FileInfo, String>(PrintDisassemble);
            disassemble.AddCommand(method);

            var all = new Command("all", "All methods in the assembly are disassembled.");
            all.Handler = CommandHandler.Create<FileInfo>(PrintDisassemble);
            disassemble.AddCommand(all);

            rootCommand.Add(disassemble);
        }

        static void PrintDisassemble(FileInfo input, String method)
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

                            List<BytecodeInstruction> tac = Bytecode.Compute(m.Body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);
                            Console.WriteLine(String.Join(System.Environment.NewLine, tac.Select(t => t.ToString())));
                            return;
                        }
                    }
                }

                Console.WriteLine("No method found.");
            }
        }

        static void PrintDisassemble(FileInfo input)
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

                        List<BytecodeInstruction> tac = Bytecode.Compute(m.Body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);
                        Console.WriteLine(String.Join(System.Environment.NewLine, tac.Select(t => "\t" + t.ToString())));
                    }
                }
            }
        }

    }
}