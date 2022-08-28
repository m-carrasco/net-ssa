using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NetSsa.Analyses;
using NetSsa.Reflection;

namespace NetSsaCli
{
    class Disassemble
    {
        enum DisassemblyType
        {
            IR,
            Ssa
        }

        enum TypeInferenceKind{
            None,
            Basic,
            Precise
        }

        public static void AddDisassasembleSubCommand(RootCommand rootCommand)
        {
            var disassemble = new Command("disassemble");

            var type = new Option<DisassemblyType>(
                "--type",
                getDefaultValue: () => DisassemblyType.IR,
                description: "Type of the dissasmbled code.");
            disassemble.AddOption(type);

            var verifySsa = new Option<bool>(
                "--verify-ssa",
                getDefaultValue: () => false,
                description: "Perform SSA correctness checks (one assignment, etc.).");
            disassemble.AddOption(verifySsa);

            var typeInference = new Option<TypeInferenceKind>(
                "--type-inference",
                getDefaultValue: () => TypeInferenceKind.None,
                description: "Type each SSA register according to stack types. Only valid if disassembling is SSA. Precise mode assumes that input assembly is linked against the current dotnet runtime. In addition, it may not work if the input depends on other assemblies than the one of the runtime.");
            disassemble.AddOption(typeInference);

            var method = new Command("method");
            method.AddArgument(new Argument<String>("method", "Method to disassemble."));
            method.Handler = CommandHandler.Create<FileInfo, DisassemblyType, bool, TypeInferenceKind, String>(PrintDisassemble);
            disassemble.AddCommand(method);

            var all = new Command("all", "All methods in the assembly are disassembled.");
            all.Handler = CommandHandler.Create<FileInfo, DisassemblyType, bool, TypeInferenceKind>(PrintDisassemble);
            disassemble.AddCommand(all);

            rootCommand.Add(disassemble);
        }

        static void PrintDisassemble(FileInfo input, DisassemblyType type, bool verifySsa, TypeInferenceKind typeInference, String method)
        {
            using (AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(input.FullName))
            {
                foreach (TypeDefinition t in assembly.MainModule.GetTypes())
                {
                    foreach (MethodDefinition m in t.Methods)
                    {
                        if (m.FullName.Equals(method))
                        {
                            Console.WriteLine(m.FullName);
                            if (!m.HasBody)
                            {
                                Console.WriteLine("Method has no body.");
                                return;
                            }

                            PrintDisassemble(m, type, verifySsa, typeInference, input.FullName);

                            return;
                        }
                    }
                }

                Console.WriteLine("No method found.");
            }
        }
        
        static void PrintDisassemble(MethodDefinition m, DisassemblyType type, bool verifySsa, TypeInferenceKind typeInference, string assemblyPath){
            IRBody irBody = Unstacker.Compute(m.Body);
            bool isSsa = DisassemblyType.Ssa.Equals(type);

            IDictionary<Register, StackType> stackTypes = null;
            if (isSsa)
            {
                Ssa.Compute(irBody);
                if (typeInference != TypeInferenceKind.None){
                    
                    StackTypeInference typeAnalysis = null;
                    if (typeInference == TypeInferenceKind.Basic){
                        typeAnalysis = new StackTypeInference(irBody);
                    } else {
                        var mlc = DefaultMetadataLoadContext.BuildMetadataLoadContextCurrentRuntime(assemblyPath);
                        TypeAdapter typeAdapter = new TypeAdapter(mlc);
                        LowestCommonAncestor lowestCommonAncestor = new LowestCommonAncestor(typeAdapter);
                        typeAnalysis = new StackTypeInference(irBody, lowestCommonAncestor);
                    }
                    
                    stackTypes = typeAnalysis.Type();
                }
            }

            var lines = irBody.Instructions.Select(t => {
                String r = t.ToString();
                if (stackTypes != null && t.Result is Register register){
                    r = r + " ; " + stackTypes[register];
                }
                return r;
            });

            var variablesLines = irBody.MemoryVariables.Select(mv => {
                return mv.Kind.ToString() + " " + mv.Name + " ; " + mv.Type.FullName;
            });

            Console.WriteLine(String.Join(System.Environment.NewLine, variablesLines));
            Console.WriteLine(String.Join(System.Environment.NewLine, lines));

            if (isSsa && verifySsa)
            {
                SsaVerifier ssaVerifier = new SsaVerifier(irBody);
                ssaVerifier.Verify();
            }
        }

        static void PrintDisassemble(FileInfo input, DisassemblyType type, bool verifySsa, TypeInferenceKind typeInference)
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

                        PrintDisassemble(m, type, verifySsa, typeInference, input.FullName);
                    }
                }
            }
        }

    }
}
