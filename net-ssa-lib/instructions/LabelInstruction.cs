using System.Collections.Generic;
using System.Text;

namespace NetSsa.Instructions
{
    // A label is:
    //  * leader of a bb
    //  * first instruction or first instruction of a TryStart or HandlerStart (ExceptionHandler)
    public class LabelInstruction : TacInstruction
    {
        public override string ToString()
        {
            return Label() + ": label";
        }
    }
}
