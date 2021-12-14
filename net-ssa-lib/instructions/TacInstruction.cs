using System.Collections.Generic;
using NetSsa.Analyses;
using System;
using System.Linq;
namespace NetSsa.Instructions
{
    public abstract class TacInstruction
    {
        public LinkedListNode<TacInstruction> Node;
        public Variable Result;
        public List<Variable> Operands = new List<Variable>();

        public abstract String Label();

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

        public static ISet<TacInstruction> GetPhiAwareSuccessors(this TacInstruction instruction)
        {
            var result = new HashSet<TacInstruction>();
            if (instruction is ControlFlowInstruction controlFlowInstruction)
            {
                foreach (var t in controlFlowInstruction.Targets.Distinct().Select(s => s.HasPhiPredecessor() ? s.GetLastPhiPredecessor() : s))
                {
                    result.Add(t);
                }
            }

            // Fall-through semantics
            var nextNode = instruction.Node.Next;
            if (nextNode != null)
            {
                var nextInst = nextNode.Value;
                result.Add(nextInst);
            }

            return result;
        }
    }
}
