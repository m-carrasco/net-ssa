using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NetSsa.IO;

namespace NetSsa.Queries
{
    public class SsaQuery
    {
        public static String GetSsaQueryBinPath()
        {
            String ssaQueryBin = Path.GetFullPath(Environment.GetEnvironmentVariable("SSA_QUERY_BIN"));

            if (!File.Exists(ssaQueryBin))
            {
                throw new FileNotFoundException("Ssa query bin is not found: " + ssaQueryBin, ssaQueryBin);
            }

            return ssaQueryBin;
        }

        public static void Query(IEnumerable<Tuple<String>> entryInstructionFacts,
                                IEnumerable<(String, String)> edgeFacts,
                                IEnumerable<(String, String)> varDefFacts,
                                out IEnumerable<(String, String)> phiLocation,
                                out IEnumerable<(String, String)> dominators,
                                out IEnumerable<(String, String)> domFrontier,
                                out IEnumerable<(String, String)> imdom,
                                out IEnumerable<(String, String)> edges)
        {
            String ssaQueryBin = GetSsaQueryBinPath();

            String factsDirectory = FileIO.GetTempDirectory("facts_directory");
            String outputDirectory = FileIO.GetTempDirectory("output_directory");

            String entryInstructionFile = Path.Join(factsDirectory, "InstSeq.entry_instruction.facts");
            FileIO.WriteToFile(entryInstructionFile, entryInstructionFacts.Cast<ITuple>());

            String edgeFile = Path.Join(factsDirectory, "InstSeq.successor.facts");
            FileIO.WriteToFile(edgeFile, edgeFacts.Cast<ITuple>());

            String varDefFile = Path.Join(factsDirectory, "InstSeq.var_def.facts");
            FileIO.WriteToFile(varDefFile, varDefFacts.Cast<ITuple>());

            String[] arguments = { "-D" + outputDirectory, "-F" + factsDirectory };
            Process process = Process.Start(ssaQueryBin, arguments);
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new SsaQueryException(ssaQueryBin, arguments, process.StandardError.ReadToEnd());
            }

            // process files
            phiLocation = FileIO.ReadFile(Path.Join(outputDirectory, "phi_location.csv")).Cast<(String, String)>();
            dominators = FileIO.ReadFile(Path.Join(outputDirectory, "dominators.csv")).Cast<(String, String)>();
            domFrontier = FileIO.ReadFile(Path.Join(outputDirectory, "dom_frontier.csv")).Cast<(String, String)>();
            edges = FileIO.ReadFile(Path.Join(outputDirectory, "edge.csv")).Cast<(String, String)>();
            imdom = FileIO.ReadFile(Path.Join(outputDirectory, "imdom.csv")).Cast<(String, String)>();
        }
    }
}
