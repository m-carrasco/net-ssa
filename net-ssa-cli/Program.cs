﻿using System;
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
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Argument<FileInfo>("input", "Assembly to process.").ExistingOnly()
            };

            List.AddListSubCommand(rootCommand);
            Disassemble.AddDisassasembleSubCommand(rootCommand);
            Datalog.AddDatalogSubCommand(rootCommand);
            PrintCfg.AddControlFlowGraphSubCommand(rootCommand);

            return rootCommand.Invoke(args);
        }

    }

}
