using System.Collections.Generic;
using System.Collections;
using System;
using NetSsa.Instructions;
using System.Linq;

namespace NetSsa.Analyses{
    public class LiveVariableAnalysis : BackwardsDataFlowAnalysis<BitArray> {
        public LiveVariableAnalysis(ControlFlowGraph cfg) : base(cfg) {}

        private int NumberOfRegisters(){
            return irBody.Registers.Count;
        }
        
        // TO-DO: Find faster implementation
        private int RegisterIndex(Register register){
            return irBody.Registers.IndexOf(register);
        }

        // TO-DO: Find faster implementation
        private bool AreEqual(BitArray a, BitArray b) {
            if (a.Length != b.Length){
                return false;
            }

            for (int i=0; i < a.Length; i++){
                if (a[i] != b[i]){
                    return false;
                }
            }

            return true;
        }

        protected override BitArray Meet(BitArray a, BitArray b) {
            return a.Or(b);
        }

        protected override BitArray Gen(TacInstruction instruction) {
            BitArray result = new BitArray(NumberOfRegisters());
            foreach (int index in instruction.Operands.OfType<Register>().Select(r => RegisterIndex(r))){
                result.Set(index, true);
            }
            
            return result;
        }

        protected override BitArray Kill(TacInstruction instruction) {
            BitArray result = new BitArray(NumberOfRegisters());

            if (instruction.Result is Register){
                result.Set(RegisterIndex((Register)instruction.Result), true);
            }

            return result;
        }
        
        protected override bool TransferInstruction(TacInstruction instruction, BitArray incomingData){
            BitArray gen = Gen(instruction);
            BitArray kill = Kill(instruction);

            // The difference of two sets S T is computed by
            // complementing the bit vector of T, and then taking the logical AND of that
            // complement, with the bit vector for S.
            
            BitArray newValue = gen.Or(kill.Not().And(incomingData));
            
            BitArray old = IN[instruction];
            if (AreEqual(old, newValue)){
                return false;
            } else {
                IN[instruction] = newValue;
                return true;
            }
        }

        protected override BitArray InitBasicBlock(TacInstruction leader){
            return new BitArray(NumberOfRegisters());
        }
    }
}