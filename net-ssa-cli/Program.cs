using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Mono.Cecil;
namespace NetSsaCli
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand{
                new Argument<FileInfo>("input", "Assembly to process.").ExistingOnly(),
            };

            addListSubCommand(rootCommand);

            var disassemble = new Command("disassemble");
            disassemble.Handler = CommandHandler.Create(() =>
            {
                Console.WriteLine("Disassembling");
            });

            rootCommand.Add(disassemble);
            rootCommand.InvokeAsync(args);
        }

        static void addListSubCommand(RootCommand rootCommand)
        {
            var list = new Command("list", "List classes or methods in the assembly.");
            var classes = new Command("classes", "List all classes in the assembly.");
            list.Add(classes);

            classes.Handler = CommandHandler.Create<FileInfo>(PrintClasses);

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
                        Console.WriteLine(nested);
                    }
                }
            }

        }
    }
}
