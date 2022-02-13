using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NetSsa.Analyses;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace MyBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<SsaByInstructionSizeBenchmark>();
            summary = BenchmarkRunner.Run<SsaByEdgeBenchmark>();
        }
    }
}
