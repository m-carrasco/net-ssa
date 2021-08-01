using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace NetSsa.Instructions
{
    public class BytecodeInstruction : TacInstruction
    {
        public BytecodeInstruction(Instruction bytecode)
        {
            Bytecode = bytecode;
        }

        public Instruction Bytecode;

        public override string ToString()
        {
            String label = Label();
            String instruction = String.Empty;
            switch (Bytecode.OpCode.Code)
            {
                case Code.Ldarg:
                case Code.Ldarg_0:
                case Code.Ldarg_1:
                case Code.Ldarg_2:
                case Code.Ldarg_3:
                case Code.Ldarg_S:
                case Code.Stloc:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                case Code.Stloc_S:
                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                case Code.Ldloc_S:
                    return VariableAssignment(label);
                case Code.Add:
                    return BinaryOperation(label, "+");
                case Code.Mul:
                    return BinaryOperation(label, "*");
                case Code.Clt:
                    return BinaryOperation(label, "<");
                case Code.Ldc_I4_0:
                    instruction = "0";
                    break;
                case Code.Ldc_I4_1:
                    instruction = "1";
                    break;
                default:
                    instruction = CecilToStringNoLabel();
                    break;
            }
            var result = Result != null ? (" " + Result.name + " =") : String.Empty;
            var operands = this.Operands.Count() > 0 ? " " + String.Format("[{0}]", String.Join(", ", this.Operands.Select(operand => operand.name))) : String.Empty;

            return String.Format("{0}:{1} {2}{3}", label, result, instruction, operands).Trim();
        }

        private string VariableAssignment(String label)
        {
            return String.Format("{0}: {1} = {2}", label, Result.name, Operands[0].name);
        }

        private string BinaryOperation(String label, String symbol)
        {
            return String.Format("{0}: {1} = {2} {3} {4}", label, Result.name, Operands[0].name, symbol, Operands[1].name);
        }

        private string CecilToStringNoLabel()
        {
            var label = Label();
            return Bytecode.ToString().Substring(label.Count() + 2).Trim();
        }

        private string Label()
        {
            return "IL_" + Bytecode.Offset.ToString("x4");
        }
    }
}