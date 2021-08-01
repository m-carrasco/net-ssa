using System.Collections.Generic;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using Mono.Cecil;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class ThreeAddressCode
    {
        public static List<TacInstruction> Compute(MethodBody body, List<Variable> variables, Dictionary<Instruction, List<Variable>> uses, Dictionary<Instruction, List<Variable>> definitions)
        {
            List<TacInstruction> tac = new List<TacInstruction>();

            var variableNameToVariable = variables.ToDictionary(variable => variable.name);

            // Create one tac instruction per each cil instruction
            foreach (Mono.Cecil.Cil.Instruction cecilBytecode in body.Instructions)
            {
                BytecodeInstruction tacInstruction = new BytecodeInstruction(cecilBytecode);

                // assign used variables as operands
                if (uses.TryGetValue(cecilBytecode, out List<Variable> cecilBytecodeUses))
                {
                    tacInstruction.Operands = cecilBytecodeUses;
                }

                // assigned defined result
                if (definitions.TryGetValue(cecilBytecode, out List<Variable> cecilBytecodeDefinitions))
                {
                    tacInstruction.Result = cecilBytecodeDefinitions.SingleOrDefault();
                }

                tac.Add(tacInstruction);
            }

            return tac;
        }


    }
}