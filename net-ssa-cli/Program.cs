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
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand{
                new Argument<FileInfo>("input", "Assembly to process.").ExistingOnly()
            };

            addListSubCommand(rootCommand);

            var disassemble = new Command("disassemble");
            disassemble.AddArgument(new Argument<String>("method", "Method to disassemble."));
            disassemble.Handler = CommandHandler.Create<FileInfo, String>(PrintDisassemble);

            rootCommand.Add(disassemble);
            rootCommand.InvokeAsync(args);
        }

        static void addListSubCommand(RootCommand rootCommand)
        {
            var list = new Command("list", "List types or methods in the assembly.");
            var classes = new Command("types", "List all types in the assembly.");
            classes.Handler = CommandHandler.Create<FileInfo>(PrintClasses);
            list.Add(classes);

            var methods = new Command("methods", "List all methods in a type or in the assembly.");
            methods.AddArgument(new Argument<String>("type", () => null, "Type to list methods. If none, it prints all methods in the assembly."));
            methods.Handler = CommandHandler.Create<FileInfo, String>(PrintMethods);
            list.Add(methods);

            rootCommand.Add(list);
        }

        static void PrintClasses(FileInfo input)
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input.FullName))
            {
                foreach (TypeDefinition type in assembly.MainModule.Types)
                {
                    Console.WriteLine(type.FullName);
                    foreach (TypeDefinition nested in type.NestedTypes)
                    {
                        Console.WriteLine(nested.FullName);
                    }
                }
            }
        }

        static void PrintMethods(FileInfo input, String type)
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input.FullName))
            {
                if (type == null)
                {
                    foreach (TypeDefinition t in assembly.MainModule.GetTypes())
                    {
                        PrintMethods(t);
                    }
                }
                else
                {
                    var targetType = assembly.MainModule.GetTypes().Where(t => t.FullName.Equals(type)).DefaultIfEmpty().Single();

                    if (targetType == null)
                        return;

                    PrintMethods(targetType);
                }
            }
        }

        static void PrintMethods(TypeDefinition type)
        {
            foreach (MethodDefinition methodDefinition in type.Methods)
            {
                Console.WriteLine(methodDefinition.FullName);
            }
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

                            List<TacInstruction> tac = ThreeAddressCode.Compute(m.Body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);
                            Console.WriteLine(String.Join(System.Environment.NewLine, tac.Select(t => t.ToString())));
                            return;
                        }
                    }
                }

                Console.WriteLine("No method found.");
            }
        }

    }

}
