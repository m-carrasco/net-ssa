using System.Collections.Generic;
using NetSsa.Analyses;

namespace NetSsa.Instructions
{
    public class PhiInstruction : TacInstruction
    {
        public List<TacInstruction> Incoming = new List<TacInstruction>();
    }
}
