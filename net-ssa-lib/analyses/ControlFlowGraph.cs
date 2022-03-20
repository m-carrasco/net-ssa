using System.Collections.Generic;
using System.Linq;
using System;
using NetSsa.Instructions;
using DotNetGraph.Edge;
using DotNetGraph.Extensions;
using DotNetGraph.Node;
using DotNetGraph;
using System.Drawing;
namespace NetSsa.Analyses
{
    public class ControlFlowGraph
    {
        public readonly IRBody IRBody;
        public ControlFlowGraph(IRBody irBody)
        {
            IRBody = irBody;
            ComputeLeaders();
        }

        private ISet<TacInstruction> _entries = new HashSet<TacInstruction>();

        private ISet<TacInstruction> _leaders = new HashSet<TacInstruction>();

        public ISet<TacInstruction> Entries()
        {
            return _entries;
        }

        private void SetEntries()
        {
            foreach (TacInstruction e in this.ExceptionHandlerEntries())
            {
                _entries.Add(e);
            }
            _entries.Add(IRBody.Instructions.First.Value);
        }

        // A leader is the first instruction in a basic block.
        public ISet<TacInstruction> Leaders()
        {
            return _leaders;
        }

        public TacInstruction GetLeader(TacInstruction instruction)
        {
            if (_leaders.Contains(instruction))
            {
                return instruction;
            }

            foreach (TacInstruction leader in _leaders)
            {
                ISet<TacInstruction> instructions = BasicBlockInstructions(leader).ToHashSet();

                if (instructions.Contains(instruction))
                {
                    return leader;
                }
            }

            throw new ArgumentException("The instruction does not belong to any basic block: " + instruction);
        }

        public IEnumerable<TacInstruction> ExceptionHandlerEntries()
        {
            foreach (var exceptionHandler in IRBody.ExceptionHandlers)
            {
                var filterStart = exceptionHandler.FilterStart;
                if (filterStart != null)
                {
                    yield return filterStart;
                }

                yield return exceptionHandler.HandlerStart;
            }
        }

        public void ComputeLeaders()
        {
            _leaders.Clear();
            _entries.Clear();
            SetEntries();

            foreach (TacInstruction entry in _entries)
            {
                if (entry is PhiInstruction)
                {
                    throw new NotSupportedException("A phi instruction cannot be an entry: " + entry.ToString());
                }

                _leaders.Add(entry);
            }

            var cfInstructions = IRBody.Instructions.OfType<ControlFlowInstruction>();
            foreach (ControlFlowInstruction cfInstruction in cfInstructions)
            {
                foreach (TacInstruction explicitTarget in cfInstruction.Targets)
                {
                    TacInstruction target = explicitTarget;
                    _leaders.Add(target);
                }

                LinkedListNode<TacInstruction> nextNode = cfInstruction.Node.Next;
                if (nextNode != null)
                {
                    TacInstruction nextToCf = nextNode.Value;
                    _leaders.Add(nextToCf);
                }
            }
        }

        public IEnumerable<TacInstruction> BasicBlockInstructions(TacInstruction leader)
        {
            LinkedListNode<TacInstruction> current = leader.Node;
            do
            {
                yield return current.Value;
                current = current.Next;
            } while (current != null && !_leaders.Contains(current.Value));
        }

        public IList<TacInstruction> BasicBlockSuccessors(TacInstruction leader)
        {
            var successors = new HashSet<TacInstruction>();

            TacInstruction last = BasicBlockInstructions(leader).Last();

            // Explicit successors
            if (last is ControlFlowInstruction controlFlowInstruction)
            {
                foreach (TacInstruction target in controlFlowInstruction.Targets)
                {
                    successors.Add(target);
                }
            }

            // Implicit successor
            if (last is BytecodeInstruction bytecodeInstruction)
            {
                if (ControlFlowInstruction.CanFallThrough(bytecodeInstruction.OpCode.FlowControl))
                {
                    LinkedListNode<TacInstruction> nextNode = last.Node.Next;
                    if (nextNode != null)
                    {
                        successors.Add(nextNode.Value);
                    }
                }
            }

            if (last is PhiInstruction)
            {
                throw new NotSupportedException("The last instruction of a basic block " + leader.ToString() + "cannot be a phi instruction: " + last.ToString());
            }

            if (last is LabelInstruction)
            {
                throw new NotSupportedException("The last instruction of a basic block " + leader.ToString() + "cannot be a label instruction: " + last.ToString());
            }

            return successors.ToList();
        }

        public IList<TacInstruction> BasicBlockPredecessors(TacInstruction leader)
        {
            IList<TacInstruction> result = new List<TacInstruction>();
            foreach (TacInstruction basicBlockLeader in _leaders)
            {
                ISet<TacInstruction> successors = BasicBlockSuccessors(basicBlockLeader).ToHashSet();

                if (successors.Contains(leader))
                {
                    result.Add(basicBlockLeader);
                }
            }

            return result;
        }
    }

    public static class ControlFlowGraphExtensions
    {
        public static String SerializeDotFile(this ControlFlowGraph cfg)
        {
            DotGraph directedGraph = new DotGraph(cfg.IRBody.CilBody != null ? cfg.IRBody.CilBody.Method.FullName : "Control flow graph", true);

            foreach (TacInstruction leader in cfg.Leaders())
            {
                String label = String.Join(System.Environment.NewLine, cfg.BasicBlockInstructions(leader));

                DotNode node = new DotNode(leader.ToString());
                node.Label = label;

                directedGraph.Elements.Add(node);
            }

            foreach (TacInstruction leader in cfg.Leaders())
            {
                IEnumerable<TacInstruction> successors = cfg.BasicBlockSuccessors(leader);
                foreach (TacInstruction successor in successors)
                {
                    DotEdge edge = new DotEdge(leader.ToString(), successor.ToString());
                    directedGraph.Elements.Add(edge);
                }
            }

            DotNode entry = new DotNode("Entry");
            entry.Color = Color.Blue;
            directedGraph.Elements.Add(entry);

            foreach (TacInstruction e in cfg.Entries())
            {
                DotEdge edge = new DotEdge("Entry", e.ToString());
                directedGraph.Elements.Add(edge);
            }

            return directedGraph.Compile(true);
        }
    }
}
