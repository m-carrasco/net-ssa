using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class BytecodeBody
    {
        public MethodBody CilBody;
        public List<Variable> Variables;

        public List<Variable> Arguments
        {
            get { return Variables.Where(v => v.IsArgumentVariable()).ToList(); }
        }

        public LinkedList<BytecodeInstruction> Instructions;
    }

    public class Bytecode
    {
        public static BytecodeBody Compute(MethodBody body, List<Variable> variables, Dictionary<Instruction, List<Variable>> uses, Dictionary<Instruction, List<Variable>> definitions)
        {
            LinkedList<BytecodeInstruction> bytecodes = new LinkedList<BytecodeInstruction>();

            var variableNameToVariable = variables.ToDictionary(variable => variable.Name);

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

                bytecodes.AddLast(bytecode);
            }

            return new BytecodeBody()
            {
                CilBody = body,
                Instructions = bytecodes,
                Variables = variables
            };
        }

        public static BytecodeBody Compute(MethodBody body)
        {
            VariableDefUse.Compute(body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);
            return Bytecode.Compute(body, variables, uses, definitions);
        }
    }
}
