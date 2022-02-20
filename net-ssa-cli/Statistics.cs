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
    class Statistics
    {
        enum Format
        {
            Plain,
            Csv
        }
        public static void AddStatisticsSubCommand(RootCommand rootCommand)
        {
            var stats = new Command("stats", "Calculate basic method stats.");
            stats.AddArgument(new Argument<String>("method", () => null, "Method to compute stats. If none, stats are calculated for every method in the assembly."));

            var type = new Option<Format>(
                "--format",
                getDefaultValue: () => Format.Plain,
                description: "Output format.");
            stats.AddOption(type);
            stats.Handler = CommandHandler.Create<FileInfo, String, Format>(PrintStats);

            rootCommand.Add(stats);
        }

        static void PrintStats(FileInfo input, String method, Format format)
        {
            // , is not a good separator because it appears in the name
            String separator = ";";
            if (format == Format.Csv)
            {
                Console.WriteLine("name" + separator + "instructions" + separator + "edges");
            }

            Iterator.IterateMethods(input, (MethodDefinition methodDef) =>
            {
                bool printStats = String.IsNullOrEmpty(method) ? true : methodDef.FullName.Equals(method);

                if (!printStats)
                {
                    return;
                }

                int instructions = methodDef.HasBody ? methodDef.Body.Instructions.Count : 0;
                int edgesCount = methodDef.HasBody ? CountEdges(methodDef.Body) : 0;

                switch (format)
                {
                    case Format.Plain:
                        Console.WriteLine("Method: " + methodDef.FullName);
                        Console.WriteLine("\tInstructions: " + instructions);
                        Console.WriteLine("\tNon-exceptional edges: " + edgesCount);
                        break;
                    case Format.Csv:
                        Console.WriteLine(String.Format("\"{0}\"{1}{2}{3}{4}", methodDef.FullName, separator, instructions, separator, edgesCount));
                        break;
                }
            });
        }

        static int CountEdges(MethodBody body)
        {
            IDictionary<Instruction, ISet<Instruction>> edges = new Dictionary<Instruction, ISet<Instruction>>();
            Successor.NonExceptionalSuccessor(edges, body);
            return edges.Select(kv => kv.Value.Count).Sum();
        }
    }
}
