using NetSsa.Analyses;
using System.Collections.Generic;

namespace NetSsa.Instructions
{
    public abstract class TacInstruction
    {
        public Variable Result;
        public List<Variable> Operands = new List<Variable>();
    }
}