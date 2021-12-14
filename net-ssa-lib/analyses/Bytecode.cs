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

        public LinkedList<TacInstruction> Instructions;
    }

    public class Bytecode
    {
        public static BytecodeBody Compute(MethodBody body, List<Variable> variables, Dictionary<Instruction, List<Variable>> uses, Dictionary<Instruction, List<Variable>> definitions)
        {
            LinkedList<TacInstruction> bytecodes = new LinkedList<TacInstruction>();

            var variableNameToVariable = variables.ToDictionary(variable => variable.Name);

            var cecilToBytecode = new Dictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>>();

            // Create one tac instruction per each cil instruction
            foreach (Mono.Cecil.Cil.Instruction cecilBytecode in body.Instructions)
            {
                var code = cecilBytecode.OpCode.Code;
                var isControlFlow = ControlFlowInstruction.IsControlFlowSequenceCode(code);

                BytecodeInstruction bytecode = isControlFlow ? new ControlFlowInstruction(cecilBytecode) : new BytecodeInstruction(cecilBytecode);

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

                var node = bytecodes.AddLast(bytecode);

                cecilToBytecode[cecilBytecode] = node;
                bytecode.Node = node;
            }

            // Set targets of net-ssa control flow instructions.
            SetControlFlowTargets(body, cecilToBytecode);

            return new BytecodeBody()
            {
                CilBody = body,
                Instructions = bytecodes,
                Variables = variables
            };
        }

        private static void SetControlFlowTargets(MethodBody body, Dictionary<Instruction, LinkedListNode<TacInstruction>> cecilToBytecode)
        {
            foreach (Mono.Cecil.Cil.Instruction cecilBytecode in body.Instructions)
            {
                var code = cecilBytecode.OpCode.Code;
                if (!ControlFlowInstruction.IsControlFlowSequenceCode(code))
                {
                    continue;
                }

                ControlFlowInstruction controlFlowInstruction = (ControlFlowInstruction)cecilToBytecode[cecilBytecode].Value;

                var operand = cecilBytecode.Operand;
                if (operand is Instruction target)
                {
                    controlFlowInstruction.Targets.Add(cecilToBytecode[cecilBytecode].Value);
                }
                else if (operand is Instruction[] targets)
                {
                    controlFlowInstruction.Targets.AddRange(targets.Select(t => cecilToBytecode[t].Value));
                }
                else if (operand != null)
                {
                    throw new NotSupportedException("Unhandled case for control flow instruction: " + operand.GetType());
                }
            }
        }

        public static BytecodeBody Compute(MethodBody body)
        {
            VariableDefUse.Compute(body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);
            return Bytecode.Compute(body, variables, uses, definitions);
        }
    }
}
