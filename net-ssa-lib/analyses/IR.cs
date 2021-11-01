using System.Collections.Generic;
using Mono.Cecil.Cil;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class IR
    {
        public static BytecodeBody Compute(MethodBody body)
        {
            VariableDefUse.Compute(body, out List<Variable> variables, out Dictionary<Instruction, List<Variable>> uses, out Dictionary<Instruction, List<Variable>> definitions);
            BytecodeBody bytecodeBody = Bytecode.Compute(body, variables, uses, definitions);
            VariableDefinitionsToUses(bytecodeBody.Instructions);
            return bytecodeBody;
        }

        public static void VariableDefinitionsToUses(BytecodeBody bytecodeBody)
        {
            VariableDefinitionsToUses(bytecodeBody.Instructions);
        }

        public static void VariableDefinitionsToUses(LinkedList<BytecodeInstruction> bytecodeInstructions)
        {
            foreach (BytecodeInstruction bytecode in bytecodeInstructions)
            {
                var cil = bytecode.Bytecode;
                SwitchDefinitionToUse(cil, bytecode.Operands, new List<Variable>() { bytecode.Result }, ref bytecode.Result);
            }
        }

        public static void VariableDefinitionsToUses(Dictionary<Instruction, List<Variable>> uses, Dictionary<Instruction, List<Variable>> definitions)
        {
            foreach (Instruction cil in uses.Keys)
            {
                Variable dummy = null;
                SwitchDefinitionToUse(cil, uses[cil], definitions[cil], ref dummy);
            }
        }

        private static void SwitchDefinitionToUse(Instruction cil, List<Variable> uses, List<Variable> definitions, ref Variable result)
        {
            switch (cil.OpCode.Code)
            {
                case Code.Stloc_3:
                case Code.Stloc_2:
                case Code.Stloc_1:
                case Code.Stloc_0:
                case Code.Stloc:
                case Code.Stloc_S:
                case Code.Starg:
                case Code.Starg_S:
                    var r = definitions[0];
                    definitions.Clear();
                    uses.Insert(0, r);
                    result = null;
                    break;
            }
        }
    }
}
