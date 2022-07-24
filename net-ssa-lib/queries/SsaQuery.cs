using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.InteropServices;
using NetSsa.IO;

namespace NetSsa.Queries
{
    public class SsaQuery
    {
        public static readonly String SsaQueryBinPath = GetSsaQueryBinPath();

        private static String GetTargetSuffix(){
            OSPlatform platform;
            String suffix;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)){
                platform = OSPlatform.Linux;
                suffix = "-linux-x86-64";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)){
                platform = OSPlatform.Windows;
                suffix = "-windows-x86-64.exe";
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)){
                platform = OSPlatform.OSX;
                suffix = "macos-x86-64-arm64";
            } else {
                throw new NotImplementedException("Unsupported Operating System");
            }
        
            if (platform.Equals(OSPlatform.Linux) || platform.Equals(OSPlatform.Windows)){
                if (RuntimeInformation.OSArchitecture != Architecture.X64){
                    throw new NotImplementedException("Unsupported Architecture: " + RuntimeInformation.OSArchitecture);
                }
            } else if (platform.Equals(OSPlatform.OSX)){
                if (RuntimeInformation.OSArchitecture != Architecture.X64 && RuntimeInformation.OSArchitecture != Architecture.Arm64){
                    throw new NotImplementedException("Unsupported Architecture: " + RuntimeInformation.OSArchitecture);
                }
            }

            return suffix;
        }

        public static String GetSsaQueryBinPath()
        {
            String suffix = GetTargetSuffix();
            string directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
            string result = Path.Combine(directory, "ssa-query" + suffix);
            if (!File.Exists(result))
            {
                throw new FileNotFoundException(result);
            }

            return result;
        }

        public class Result
        {
            public IEnumerable<(String, String)> PhiLocation;
            public IEnumerable<(String, String)> ImmediateDominator;
        }

        public static Result Query(IEnumerable<Tuple<String>> entryInstructionFacts,
                                IEnumerable<(String, String)> edgeFacts,
                                IEnumerable<(String, String)> varDefFacts)
        {
            String ssaQueryBin = SsaQuery.SsaQueryBinPath;

            DirectoryInfo directory = FileIO.GetTempDirectory();

            String factsDirectory = Directory.CreateDirectory(Path.Combine(new string[] { directory.FullName, "facts_directory" })).FullName;
            String outputDirectory = Directory.CreateDirectory(Path.Combine(new string[] { directory.FullName, "output_directory" })).FullName;

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

            Result result = new Result();
            result.PhiLocation = FileIO.ReadFile(Path.Join(outputDirectory, "phi_location.csv")).Cast<(String, String)>();
            result.ImmediateDominator = FileIO.ReadFile(Path.Join(outputDirectory, "imdom.csv")).Cast<(String, String)>();

            // Delete folder and do not block while doing it
            Task.Run(() => { Directory.Delete(directory.FullName, true); });

            return result;
        }
    }
}
