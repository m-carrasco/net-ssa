using System.Collections.Generic;
using System.Text;

namespace NetSsa.Instructions
{
    public class PhiInstruction : TacInstruction
    {
        public uint Id;

        public List<TacInstruction> Incoming = new List<TacInstruction>();

        public override string ToString()
        {
            string[] pairs = new string[Incoming.Count];
            for (int i = 0; i < Incoming.Count; i++)
            {
                pairs[i] = "(" + Operands[i].Name + "," + Incoming[i].Label() + ")";
            }

            return Label() + ": " + Result.Name + " = phi [" + string.Join(",", pairs) + "]";
        }

        public override string Label()
        {
            return "PHI_" + Id.ToString("x4");
        }
    }
}
