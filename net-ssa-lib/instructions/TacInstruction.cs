using System.Collections.Generic;
using NetSsa.Analyses;
using System;
using System.Linq;

namespace NetSsa.Instructions
{
    public abstract class TacInstruction
    {
        public uint Offset;
        public LinkedListNode<TacInstruction> Node;
        public virtual string Label()
        {
            return "L_" + Offset.ToString("x4");
        }

        public IList<ValueContainer> Operands = new List<ValueContainer>();
        public ValueContainer Result;
    }

    public static class Extensions
    {
        public static bool HasPhiPredecessor(this TacInstruction instruction)
        {
            LinkedListNode<TacInstruction> currentNode = instruction.Node.Previous;

            if (currentNode != null && currentNode.Value is PhiInstruction)
                return true;

            return false;
        }

        public static PhiInstruction GetLastPhiPredecessor(this TacInstruction instruction)
        {
            // assume instruction.HasPhiPredecessor();

            LinkedListNode<TacInstruction> nextToTest = instruction.Node.Previous.Previous;
            LinkedListNode<TacInstruction> previousPhi = instruction.Node.Previous;

            while (nextToTest != null && nextToTest.Value is PhiInstruction)
            {
                previousPhi = nextToTest;
                nextToTest = nextToTest.Previous;
            }

            return (PhiInstruction)previousPhi.Value;
        }

    }
}
