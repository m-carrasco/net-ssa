using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using NetSsa.Facts;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class Bytecode
    {
        public static List<BytecodeInstruction> Compute(MethodBody body, List<Variable> variables, Dictionary<Instruction, List<Variable>> uses, Dictionary<Instruction, List<Variable>> definitions)
        {
            List<BytecodeInstruction> bytecodes = new List<BytecodeInstruction>();

            var variableNameToVariable = variables.ToDictionary(variable => variable.name);

            // Create one tac instruction per each cil instruction
            foreach (Mono.Cecil.Cil.Instruction cecilBytecode in body.Instructions)
            {
                BytecodeInstruction bytecode = new BytecodeInstruction(cecilBytecode);

                // assign used variables as operands
                if (uses.TryGetValue(cecilBytecode, out List<Variable> cecilBytecodeUses))
                {
                    bytecode.Operands = cecilBytecodeUses;
                }

                // assigned defined result
                if (definitions.TryGetValue(cecilBytecode, out List<Variable> cecilBytecodeDefinitions))
                {
                    bytecode.Result = cecilBytecodeDefinitions.SingleOrDefault();
                }

                bytecodes.Add(bytecode);
            }

            return bytecodes;
        }

        public static List<BytecodeInstruction> Compute(MethodBody body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions)
        {
            VariableDefUse.Compute(body, out variables, out uses, out definitions);
            return Bytecode.Compute(body, variables, uses, definitions);
        }
    }
}
