using System.Collections.Generic;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public abstract class ValueContainer
    {
        public string Name;

        protected IList<TacInstruction> _definitions = new List<TacInstruction>();
        protected IList<TacInstruction> _uses = new List<TacInstruction>();

        public IList<TacInstruction> Definitions
        {
            get => _definitions;
            set => _definitions = value;
        }

        public IList<TacInstruction> Uses
        {
            get => _uses;
            set => _uses = value;
        }

        public void AddDefinition(TacInstruction instruction, bool updateInst = true)
        {
            Definitions.Add(instruction);
            if (updateInst)
                instruction.Result = this;
        }

        public void RemoveDefinition(TacInstruction instruction, bool updateInst = true)
        {
            Definitions.Remove(instruction);
            if (updateInst)
                instruction.Result = null;
        }

        public void AddUse(TacInstruction instruction, bool updateInst = true)
        {
            Uses.Add(instruction);
            if (updateInst)
                instruction.Operands.Add(this);
        }

        public void RemoveUse(TacInstruction instruction, bool updateInst = true)
        {
            Uses.Remove(instruction);
            if (updateInst)
                instruction.Operands.Remove(this);
        }

        public ValueContainer(string name)
        {
            Name = name;
        }
    }
}
