using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using NetSsa.Analyses;
using NetSsa.Instructions;

namespace NetSsa.Facts
{
    public class SsaFacts
    {
        public static String Label(Instruction instruction)
        {
            return "IL_" + instruction.Offset.ToString("x4");
        }

        public static IEnumerable<(String, String)> Successor(MethodBody methodBody)
        {
            var successors = new Dictionary<Instruction, ISet<Instruction>>();
            Analyses.Successor.NonExceptionalSuccessor(successors, methodBody);

            foreach (Instruction instruction in methodBody.Instructions)
            {
                if (!successors.ContainsKey(instruction))
                    continue;

                String currentLabel = Label(instruction);
                foreach (var successor in successors[instruction])
                {
                    yield return (currentLabel, Label(successor));
                }
            }

            yield break;
        }

        public static IEnumerable<(String, String)> ExceptionalSuccessor(MethodBody methodBody)
        {
            var successors = new Dictionary<Instruction, ISet<Instruction>>();
            Analyses.Successor.ExceptionalSuccessors(successors, methodBody);

            foreach (Instruction instruction in methodBody.Instructions)
            {
                if (!successors.ContainsKey(instruction))
                    continue;

                String currentLabel = Label(instruction);
                foreach (var successor in successors[instruction])
                {
                    yield return (currentLabel, Label(successor));
                }
            }

            yield break;
        }

        public static IEnumerable<(String, String)> VarDefRegisters(IRBody irBody)
        {
            foreach (var reg in irBody.Registers)
            {
                foreach (var tac in reg.Definitions)
                {
                    yield return (reg.Name, tac.Label());
                }
            }
        }

        public static IEnumerable<Tuple<String>> EntryInstruction(MethodBody methodBody)
        {
            yield return Tuple.Create<String>(Label(methodBody.Instructions.First()));

            foreach (var exceptionHandler in methodBody.ExceptionHandlers)
            {
                var filterStart = exceptionHandler.FilterStart;
                var handlerStart = exceptionHandler.HandlerStart;
                if (filterStart != null)
                {
                    yield return Tuple.Create<String>(Label(filterStart));
                }

                yield return Tuple.Create<String>(Label(handlerStart));
            }

            yield break;
        }
    }
}
