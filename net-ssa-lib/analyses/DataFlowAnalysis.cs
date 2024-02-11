using System.Collections.Generic;
using NetSsa.Instructions;
using System.Linq;

namespace NetSsa.Analyses
{
    public abstract class BackwardsDataFlowAnalysis<Data> 
    {
        public BackwardsDataFlowAnalysis(ControlFlowGraph cfg){
            this.cfg = cfg;
            this.irBody = cfg.IRBody;
        }

        protected ControlFlowGraph cfg;
        protected IRBody irBody;

        protected abstract Data Meet(Data a, Data b); // This is expected to create a copy of the input data
        protected virtual Data MeetSuccessors(TacInstruction leader){
            IList<TacInstruction> successors = cfg.BasicBlockSuccessors(leader);
            Data result = IN[successors[0]];
            for (int i = 1; i < successors.Count; i++){
                result = Meet(result, IN[successors[i]]);
            }
            return result;
        }

        protected abstract Data Gen(TacInstruction leader);
        protected abstract Data Kill(TacInstruction leader);
        protected abstract bool TransferInstruction(TacInstruction instruction, Data incomingData);

        protected virtual void Transfer(TacInstruction leader, Data incomingData, ref bool changed){
            IList<TacInstruction> instructions = cfg.BasicBlockInstructions(leader).ToList();
            
            bool currentChanged = TransferInstruction(instructions[instructions.Count-1], incomingData);
            for (int i=instructions.Count-2; i >= 0; i--){
                currentChanged = currentChanged || TransferInstruction(instructions[i], IN[instructions[i+1]]);
            }

            changed = currentChanged;
        }

        protected abstract Data InitBasicBlock(TacInstruction leader);

        public IDictionary<TacInstruction, Data> IN = new Dictionary<TacInstruction, Data>();
        public IDictionary<TacInstruction, Data> OUT = new Dictionary<TacInstruction, Data>();

        protected virtual IEnumerable<TacInstruction> GetExitBasicBlocks(){
            foreach (TacInstruction leader in cfg.Leaders()){
                if (cfg.BasicBlockSuccessors(leader).Count == 0){
                    yield return leader;
                }
            }
        }

        protected void InitializeBasicBlocks(){
            foreach (TacInstruction leader in cfg.Leaders()){
                foreach (TacInstruction instruction in cfg.BasicBlockInstructions(leader)){
                    IN[instruction] = InitBasicBlock(leader);
                }
            }
        }

        public void Flow(){
            InitializeBasicBlocks();
            bool inChanged;
            do {
                inChanged = false;
                foreach (TacInstruction leader in cfg.Leaders().Except(GetExitBasicBlocks())){
                    OUT[leader] = MeetSuccessors(leader);
                    Transfer(leader, OUT[leader], ref inChanged);  
                }
            } while (inChanged);
        }
    }

}