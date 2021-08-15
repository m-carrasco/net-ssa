using System;
using System.Linq;
using Mono.Cecil.Cil;

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
                case Code.Ceq:
                    return BinaryOperation(label, "==");
                case Code.Bge:
                    return BinaryConditionalBranch(label, ">=");
                case Code.Ble:
                    return BinaryConditionalBranch(label, "<=");
                case Code.Bne_Un:
                    return BinaryConditionalBranch(label, "!=", "[unsigned]");
                case Code.Brtrue_S:
                    return BinaryConditionalBranch(label, "==", Operands[0].name, "true");
                case Code.Ldc_I4_0:
                    instruction = "0";
                    break;
                case Code.Ldc_I4_1:
                    instruction = "1";
                    break;
                case Code.Ldc_I4_2:
                    instruction = "2";
                    break;
                case Code.Ldc_I4_5:
                    instruction = "5";
                    break;
                case Code.Ldc_I4_M1:
                    instruction = "-1";
                    break;
                case Code.Ret:
                    return Ret(label);
                case Code.Throw:
                    return Throw(label);
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

        private string BinaryConditionalBranch(String label, String symbol, String message)
        {
            return BinaryConditionalBranch(label, symbol) + " " + message;
        }

        private string BinaryConditionalBranch(String label, String symbol)
        {
            return BinaryConditionalBranch(label, symbol, Operands[0].name, Operands[1].name);
        }

        private string BinaryConditionalBranch(String label, String symbol, String operand0, String operand1)
        {
            return String.Format("{0}: br {1} if {2} {3} {4}", label, Label(((Instruction)this.Bytecode.Operand)), operand0, symbol, operand1);
        }


        private string Ret(String label)
        {
            return String.Format("{0}: ret {1}", label, Operands.Count() == 0 ? String.Empty : Operands[0].name);
        }

        private string Throw(String label)
        {
            return String.Format("{0}: throw {1}", label, Operands[0].name);
        }

        private string CecilToStringNoLabel()
        {
            var label = Label();
            return Bytecode.ToString().Substring(label.Count() + 2).Trim();
        }

        private string Label()
        {
            return Label(this.Bytecode);
        }

        private string Label(Instruction instruction)
        {
            return "IL_" + instruction.Offset.ToString("x4");
        }
    }
}
