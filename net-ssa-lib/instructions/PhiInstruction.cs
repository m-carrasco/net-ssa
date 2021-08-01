using NetSsa.Analyses;
using System.Collections.Generic;

namespace NetSsa.Instructions
{
    public class PhiInstruction : TacInstruction
    {
        public List<TacInstruction> Incoming = new List<TacInstruction>();
    }
}