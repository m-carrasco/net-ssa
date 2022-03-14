using System;
using System.Linq;
using Mono.Cecil.Cil;
using System.Text;

namespace NetSsa.Instructions
{
    public class BytecodeInstruction : TacInstruction
    {
        public BytecodeInstruction(Instruction bytecode)
        {
            this.OpCode = bytecode.OpCode;
            this.EncodedOperand = bytecode.Operand;
            this.Offset = bytecode.Offset;
        }

        public BytecodeInstruction(OpCode opCode, Object operand, int offset)
        {
            this.OpCode = opCode;
            this.EncodedOperand = operand;
            this.Offset = offset;
        }

        public int Offset;

        public Instruction Bytecode;

        public OpCode OpCode;

        // This is the same value that Mono.Cecil.Instruction has in its 'operand' field.
        // EncodedOperand is the value encoded in the instruction not a value consumed from the stack.
        public Object EncodedOperand;

        public override string Label()
        {
            return "IL_" + Offset.ToString("x4");
        }

        public override string ToString()
        {
            string label = Label();
            string instruction = CecilToString();

            string result = Result != null ? (" " + Result.Name + " =") : String.Empty;
            string operands = this.Operands.Count() > 0 ? " " + String.Format("[{0}]", String.Join(", ", this.Operands.Select(operand => operand.Name))) : String.Empty;

            return String.Format("{0}:{1} {2}{3}", label, result, instruction, operands).Trim();
        }

        private string CecilToString()
        {
            var instruction = new StringBuilder();

            instruction.Append(OpCode.Name);

            bool isControlFlow = this is ControlFlowInstruction;

            if (EncodedOperand == null && !isControlFlow)
                return instruction.ToString();

            instruction.Append(' ');

            if (isControlFlow)
            {
                ControlFlowInstruction cfi = (ControlFlowInstruction)this;
                for (int i = 0; i < cfi.Targets.Count; i++)
                {
                    if (i > 0)
                        instruction.Append(',');
                    instruction.Append(cfi.Targets[i].Label());
                }

                return instruction.ToString();
            }

            switch (OpCode.OperandType)
            {
                case OperandType.InlineString:
                    instruction.Append('\"');
                    instruction.Append(EncodedOperand);
                    instruction.Append('\"');
                    break;
                default:
                    instruction.Append(EncodedOperand);
                    break;
            }

            return instruction.ToString();
        }
    }
}
