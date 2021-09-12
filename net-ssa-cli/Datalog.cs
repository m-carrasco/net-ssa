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

                var body = m.Body;
                var varDef = SsaFacts.VarDef(body);
                var edge = SsaFacts.Edge(body);
                var start = SsaFacts.Start(body);

                Console.WriteLine("edge: " + (edge.Count() == 0 ? "empty" : ""));
                foreach (var t in edge)
                {
                    Console.WriteLine(String.Format("\t{0} {1}", t.Item1, t.Item2));
                }

                Console.WriteLine("var_def: " + (varDef.Count() == 0 ? "empty" : ""));
                foreach (var t in varDef)
                {
                    Console.WriteLine(String.Format("\t{0} {1}", t.Item1, t.Item2));
                }

                SsaQuery.Query(start, edge, varDef, out IEnumerable<(String, String)> phiLocation, out IEnumerable<(String, String)> dominators, out IEnumerable<(String, String)> domFrontier);

                Console.WriteLine("phi_location: " + (phiLocation.Count() == 0 ? "empty" : ""));
                foreach (var t in phiLocation)
                {
                    Console.WriteLine(String.Format("\t{0} {1}", t.Item1, t.Item2));
                }

                Console.WriteLine("dominators: " + (dominators.Count() == 0 ? "empty" : ""));
                foreach (var t in dominators)
                {
                    Console.WriteLine(String.Format("\t{0} {1}", t.Item1, t.Item2));
                }

                Console.WriteLine("dominance_frontier: " + (domFrontier.Count() == 0 ? "empty" : ""));
                foreach (var t in domFrontier)
                {
                    Console.WriteLine(String.Format("\t{0} {1}", t.Item1, t.Item2));
                }
            });
        }

        static void PrintQueryAll(FileInfo input, String query)
        {
            Iterator.IterateMethods(input, m =>
            {
                if (!m.HasBody)
                {
                    return;
                }

                Console.WriteLine("Method: " + m.FullName);

                var body = m.Body;
                var varDef = SsaFacts.VarDef(body);
                var edge = SsaFacts.Edge(body);
                var start = SsaFacts.Start(body);

                SsaQuery.Query(start, edge, varDef, out IEnumerable<(String, String)> phiLocation, out IEnumerable<(String, String)> dominators, out IEnumerable<(String, String)> domFrontier);

                Console.WriteLine("phi_location: " + (phiLocation.Count() == 0 ? "empty" : ""));
                foreach (var t in phiLocation)
                {
                    Console.WriteLine(String.Format("\t{0} {1}", t.Item1, t.Item2));
                }
            });
        }
    }
}