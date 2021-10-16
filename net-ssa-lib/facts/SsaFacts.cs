using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using NetSsa.Analyses;

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

        public static IEnumerable<(String, String)> VarDef(MethodBody methodBody)
        {
            VariableDefUse.Compute(methodBody, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);

            return VarDef(definitions, uses, methodBody.Instructions);
        }

        public static IEnumerable<(String, String)> VarDef(Dictionary<Instruction, List<Variable>> definitions, Dictionary<Instruction, List<Variable>> uses, ICollection<Instruction> instructions)
        {
            foreach (Instruction instruction in instructions)
            {
                var opcode = instruction.OpCode;
                var currentLabel = Label(instruction);

                foreach (Variable variable in definitions[instruction])
                    yield return (variable.Name, currentLabel);
            }

            yield break;
        }

        public static Tuple<String> Start(MethodBody methodBody)
        {
            return Tuple.Create<String>(Label(methodBody.Instructions.First()));
        }
    }
}
