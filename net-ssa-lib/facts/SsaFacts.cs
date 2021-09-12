using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using NetSsa.Analyses;

namespace NetSsa.Facts
{
    public class SsaFacts
    {
        private static String Quote(String s)
        {
            return "\"" + s + "\"";
        }

        public static String Label(Instruction instruction)
        {
            return "IL_" + instruction.Offset.ToString("x4");
        }

        public static IEnumerable<(String, String)> Edge(MethodBody methodBody)
        {
            foreach (Instruction instruction in methodBody.Instructions)
            {
                FlowControl flowControl = instruction.OpCode.FlowControl;
                bool hasNext = true;
                String currentLabel = Label(instruction);
                switch (instruction.OpCode.FlowControl)
                {
                    case FlowControl.Next:
                        break;
                    case FlowControl.Meta:
                        break;
                    case FlowControl.Cond_Branch:
                        if (instruction.Operand is Instruction[] targets)
                        {
                            foreach (var t in targets)
                            {
                                yield return (currentLabel, Label(t));
                            }
                        }
                        else
                        {
                            yield return (currentLabel, Label((Instruction)instruction.Operand));
                        }
                        break;
                    case FlowControl.Branch:
                        hasNext = false;
                        yield return (currentLabel, Label((Instruction)instruction.Operand));
                        break;
                    case FlowControl.Return:
                    // THIS IS WRONG WE MUST CONSIDER CATCHES
                    case FlowControl.Throw:
                        hasNext = false;
                        break;
                    case FlowControl.Call:
                        hasNext = instruction.OpCode.Code != Code.Jmp;
                        break;
                    default:
                        throw new NotImplementedException("Unhandled flow control type: " + flowControl);
                }

                if (hasNext && instruction.Next != null)
                {
                    yield return (currentLabel, Label(instruction.Next));
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
                    yield return (variable.name, currentLabel);
            }

            yield break;
        }

        public static Tuple<String> Start(MethodBody methodBody)
        {
            return Tuple.Create<String>(Label(methodBody.Instructions.First()));
        }
    }
}
