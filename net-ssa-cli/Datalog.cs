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
using NetSsa.Facts;
using NetSsa.Queries;

namespace NetSsaCli
{
    class Datalog
    {
        public static void AddDatalogSubCommand(RootCommand rootCommand)
        {
            var datalog = new Command("datalog");
            var query = new Argument<String>("query", "Datalog query to execute.")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            query.FromAmong(new String[] { "phi_location" });
            datalog.AddArgument(query);

            var method = new Command("method");
            method.AddArgument(new Argument<String>("method", "Method to process."));
            method.Handler = CommandHandler.Create<FileInfo, String, String>(PrintQueryMethod);
            datalog.AddCommand(method);

            var all = new Command("all", "All methods in the assembly to process.");
            all.Handler = CommandHandler.Create<FileInfo, String>(PrintQueryAll);
            datalog.AddCommand(all);

            rootCommand.Add(datalog);
        }

        static void PrintQueryMethod(FileInfo input, String query, String method)
        {
            Iterator.IterateMethods(input, m =>
            {
                if (!m.FullName.Equals(method))
                {
                    return;
                }

                switch (query)
                {
                    case "phi_location":
                        PhiLocMethod(m);
                        break;
                }
            });
        }

        static void PhiLocMethod(MethodDefinition m)
        {
            var body = m.Body;
            var varDef = SsaFacts.VarDef(body);
            var successor = SsaFacts.Successor(body);
            var entryInstruction = SsaFacts.EntryInstruction(body);

            PrintTuples(varDef, "var_def");
            PrintTuples(successor, "successor");

            var result = SsaQuery.Query(entryInstruction, successor, varDef);

            PrintTuples(result.PhiLocation, "phi_location");
            PrintTuples(result.ImmediateDominator, "imdom");
        }

        static void PrintTuples(IEnumerable<(string, string)> tuples, string name)
        {
            Console.WriteLine(name + ": " + (tuples.Count() == 0 ? "empty" : ""));
            foreach (var t in tuples)
            {
                Console.WriteLine(String.Format("\t{0} {1}", t.Item1, t.Item2));
            }
        }

        static void PrintQueryAll(FileInfo input, String query)
        {
            Iterator.IterateMethods(input, m =>
            {
                if (!m.HasBody)
                {
                    return;
                }

                switch (query)
                {
                    case "phi_location":
                        PhiLocAll(m);
                        break;
                }
            });
        }

        static void PhiLocAll(MethodDefinition m)
        {
            Console.WriteLine("Method: " + m.FullName);

            var body = m.Body;
            var varDef = SsaFacts.VarDef(body);
            var successor = SsaFacts.Successor(body);

            var result = SsaQuery.Query(SsaFacts.EntryInstruction(body), successor, varDef);

            PrintTuples(result.PhiLocation, "phi_location");
        }
    }
}
