using System;
using System.Linq;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace NetSsa.Instructions
{
    public class ControlFlowInstruction : BytecodeInstruction
    {
        public ControlFlowInstruction(Instruction bytecode) : base(bytecode.OpCode, null, bytecode.Offset) { }
        public ControlFlowInstruction(OpCode opCode, int offset) : base(opCode, null, offset) { }
        public static bool CanFallThrough(FlowControl flowControl)
        {
            switch (flowControl)
            {
                case FlowControl.Return:
                case FlowControl.Branch:
                case FlowControl.Throw:
                    return false;
            }
            return true;
        }

        public static bool IsControlFlowSequenceCode(Code code)
        {
            switch (code)
            {
                case Code.Br_S: break;
                case Code.Brfalse_S: break;
                case Code.Brtrue_S: break;
                case Code.Beq_S: break;
                case Code.Bge_S: break;
                case Code.Bgt_S: break;
                case Code.Ble_S: break;
                case Code.Blt_S: break;
                case Code.Bne_Un_S: break;
                case Code.Bge_Un_S: break;
                case Code.Bgt_Un_S: break;
                case Code.Ble_Un_S: break;
                case Code.Blt_Un_S: break;
                case Code.Br: break;
                case Code.Brfalse: break;
                case Code.Brtrue: break;
                case Code.Beq: break;
                case Code.Bge: break;
                case Code.Bgt: break;
                case Code.Ble: break;
                case Code.Blt: break;
                case Code.Bne_Un: break;
                case Code.Bge_Un: break;
                case Code.Bgt_Un: break;
                case Code.Ble_Un: break;
                case Code.Blt_Un: break;
                case Code.Switch: break;
                case Code.Ret: break;
                case Code.Throw: break;
                case Code.Rethrow: break;
                case Code.Leave: break;
                case Code.Leave_S: break;
                case Code.Endfilter: break;
                case Code.Endfinally: break;
                default:
                    return false;
            }
            return true;
        }

        public List<TacInstruction> Targets = new List<TacInstruction>();

    }
}
