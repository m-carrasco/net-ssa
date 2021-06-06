﻿using System;
using Mono.Cecil.Cil;
using System.Linq;
using System.Collections.Generic;
using NetSsa.Analyses;

namespace NetSsa.Facts
{
    public class SsaFacts
    {
        private static String Quote(String s)
        {
            return "\"" + s + "\"";
        }

        private static String Label(Instruction instruction)
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
                    case FlowControl.Cond_Branch:
                        yield return (currentLabel, Label((Instruction)instruction.Operand));
                        break;
                    case FlowControl.Branch:
                        hasNext = false;
                        yield return (currentLabel, Label((Instruction)instruction.Operand));
                        break;
                    case FlowControl.Return:
                        hasNext = false;
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
            var definitions = new Dictionary<Instruction, List<Variable>>();
            var uses = new Dictionary<Instruction, List<Variable>>();

            VariableDefUse.Compute(methodBody, uses, definitions);

            foreach (Instruction instruction in methodBody.Instructions)
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
