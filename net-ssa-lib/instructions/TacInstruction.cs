using System.Collections.Generic;
using NetSsa.Analyses;
using System;

namespace NetSsa.Instructions
{
    public abstract class TacInstruction
    {
        public Variable Result;
        public List<Variable> Operands = new List<Variable>();

        public abstract String Label();
    }
}
