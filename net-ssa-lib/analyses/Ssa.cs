using System.Collections.Generic;
using NetSsa.Queries;
using NetSsa.Facts;
using NetSsa.Analyses;
using NetSsa.Instructions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace NetSsa.Analyses
{
    public class Ssa
    {
        public static void Compute(MethodDefinition method, IRBody irBody)
        {
            // They don't make much sense in a register-based representation.
            ReplacePopByNop(irBody);

            ControlFlowGraph cfg = new ControlFlowGraph(irBody);

            var varDef = RegisterDefFacts(cfg);
            var entryInstructions = cfg.Entries().Select(entry => Tuple.Create<String>(entry.Label()));
            var successor = SuccessorFacts(cfg);

            SsaQuery.Result ssaResult = SsaQuery.Query(entryInstructions, successor, varDef);

            InsertPhis(cfg, ssaResult, successor);

            List<Register> newVariables = new List<Register>();

            Rename(cfg, irBody.Registers, newVariables, ssaResult.ImmediateDominator);
            irBody.Registers = newVariables;
            RemoveUnusedPhiNodes(irBody);
        }

        private static IEnumerable<(String, String)> RegisterDefFacts(ControlFlowGraph cfg)
        {
            IRBody irBody = cfg.IRBody;

            foreach (TacInstruction leader in cfg.Leaders())
            {
                foreach (TacInstruction inst in cfg.BasicBlockInstructions(leader))
                {
                    if (inst.Result is Register register)
                    {
                        yield return (register.Name, leader.Label());
                    }
                }
            }
        }

        private static IEnumerable<(String, String)> SuccessorFacts(ControlFlowGraph cfg)
        {
            foreach (TacInstruction leader in cfg.Leaders())
            {
                foreach (TacInstruction successor in cfg.BasicBlockSuccessors(leader))
                {
                    yield return (leader.Label(), successor.Label());
                }
            }
        }

        private static void RemoveUnusedPhiNodes(IRBody ssaBody)
        {
            /*
                This SSA generation algorithm is based on dominance frontiers.
                Wherever a register definition may collide with another, a phi is inserted.

                This can introduce phi nodes that are not used as follows.

                void foo(){
                    nop
                    if (....){
                        sx = iconst
                        sz = iconst
                        pop sx
                        pop sz
                    }

                    // Prior renaming stage
                    // An undefined is used here because in the false case
                    // less stack slots are used (none in this case).
                    sx = phi [sx, undefined]
                    sz = phi [sz, undefined]
                }

                We are removing theses phi nodes which are not taking into account the liveness scope
                of the stack slots.

                Alternatively, we could compute an ssa based on live values but it is more expensive.
            */
            var initials = ssaBody.Instructions.OfType<PhiInstruction>().Where(phi => phi.Result.Uses.Count == 0 || phi.Operands.Any(op => op.Equals(Register.UndefinedRegister)));
            do
            {
                ISet<PhiInstruction> visited = new HashSet<PhiInstruction>();
                Stack<PhiInstruction> sources = new Stack<PhiInstruction>();

                foreach (PhiInstruction phi in initials)
                {
                    sources.Push(phi);
                    visited.Add(phi);
                }

                while (sources.Count > 0)
                {
                    PhiInstruction current = sources.Pop();
                    Register def = (Register)current.Result;
                    foreach (PhiInstruction phiUses in def.Uses.Cast<PhiInstruction>())
                    {
                        if (!visited.Contains(phiUses))
                        {
                            sources.Push(phiUses);
                            visited.Add(phiUses);
                        }
                    }
                }

                foreach (PhiInstruction visitedPhi in visited)
                {
                    ssaBody.Instructions.Remove(visitedPhi.Node);
                    ssaBody.Registers.Remove((Register)visitedPhi.Result);
                    foreach (ValueContainer use in visitedPhi.Operands)
                    {
                        use.RemoveUse(visitedPhi, false);
                    }
                }

                // There can be a phi node that now is no longer used.
                initials = ssaBody.Instructions.OfType<PhiInstruction>().Where(phi => phi.Result.Uses.Count == 0);
            } while (initials.Count() > 0);
        }

        private static void ReplacePopByNop(IRBody body)
        {
            foreach (BytecodeInstruction pop in body.Instructions.OfType<BytecodeInstruction>().Where(i => i.OpCode.Equals(OpCodes.Pop)))
            {
                pop.OpCode = OpCodes.Nop;
                pop.Operands.First().RemoveUse(pop);
            }
        }

        private static Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> Successors(IEnumerable<(String, String)> edges, Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction)
        {
            Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> result = new Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>>();

            foreach ((String, String) e in edges)
            {
                LinkedListNode<TacInstruction> source = labelToInstruction[e.Item1];
                LinkedListNode<TacInstruction> target = labelToInstruction[e.Item2];

                var sourceInstruction = (BytecodeInstruction)source.Value;

                if (!result.TryGetValue(sourceInstruction, out ISet<LinkedListNode<TacInstruction>> successors))
                {
                    successors = new HashSet<LinkedListNode<TacInstruction>>();
                    result[sourceInstruction] = successors;
                }

                successors.Add(target);
            }
            return result;
        }

        public static void InsertPhis(
                                    ControlFlowGraph cfg,
                                    SsaQuery.Result ssaQuery,
                                    IEnumerable<(String, String)> successor)
        {
            IRBody irBody = cfg.IRBody;
            var instructions = irBody.Instructions;

            Dictionary<String, TacInstruction> leadersByLabel = cfg.Leaders().ToDictionary(t => t.Label());
            Dictionary<String, Register> nameToRegister = irBody.Registers.ToDictionary(r => r.Name);

            List<PhiInstruction> phis = new List<PhiInstruction>();
            foreach ((String, String) phiLocation in ssaQuery.PhiLocation)
            {
                String registerName = phiLocation.Item1;
                String locationLabel = phiLocation.Item2;

                Register variable = nameToRegister[registerName];
                LinkedListNode<TacInstruction> locationNode = leadersByLabel[locationLabel].Node;

                PhiInstruction phi = new PhiInstruction();
                var preds = cfg.BasicBlockPredecessors(locationNode.Value);
                foreach (var op in Enumerable.Repeat(variable, preds.Count()).OfType<ValueContainer>())
                {
                    variable.AddUse(phi);
                }
                variable.AddDefinition(phi);
                phi.Incoming = preds.ToList();
                var phiNode = new LinkedListNode<TacInstruction>(phi);
                phi.Node = phiNode;
                instructions.AddAfter(locationNode, phiNode);
                phis.Add(phi);
            }
        }

        public static void Rename(ControlFlowGraph cfg,
                                  IList<Register> variables,
                                  IList<Register> newVariables,
                                  IEnumerable<(String, String)> imDominators)
        {
            // The key is original variables
            Dictionary<Register, int> counters = new Dictionary<Register, int>();

            // The key is from the orignal set of variables
            // Variables in the stacks are the new ones.
            Dictionary<Register, Stack<Register>> stacks = new Dictionary<Register, Stack<Register>>();

            foreach (var v in variables)
            {
                counters[v] = 0;
                stacks[v] = new Stack<Register>();
            }

            Dictionary<String, TacInstruction> labelToLeader = cfg.Leaders().ToDictionary(l => l.Label());
            Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> dominatorTree = GetDominatorTree(imDominators, labelToLeader, out List<LinkedListNode<TacInstruction>> sources);

            foreach (LinkedListNode<TacInstruction> source in sources)
            {
                RenameRecursive(cfg, source, counters, newVariables, stacks, dominatorTree);
            }
        }

        private static Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> GetDominatorTree(IEnumerable<(String, String)> dominators, Dictionary<String, TacInstruction> labelToInstruction, out List<LinkedListNode<TacInstruction>> sources)
        {
            var result = new Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>>();
            sources = new List<LinkedListNode<TacInstruction>>();
            foreach ((String dominatedLabel, String dominatorLabel) in dominators)
            {
                LinkedListNode<TacInstruction> dominatedNode = labelToInstruction[dominatedLabel].Node;

                // Should we rather change souffle's result?
                if (dominatorLabel.Equals("ENTRY"))
                {
                    sources.Add(dominatedNode);
                    continue;
                }

                LinkedListNode<TacInstruction> dominatorNode = labelToInstruction[dominatorLabel].Node;
                TacInstruction dominator = dominatorNode.Value;

                if (!result.TryGetValue(dominator, out ISet<LinkedListNode<TacInstruction>> dominatedNodes))
                {
                    dominatedNodes = new HashSet<LinkedListNode<TacInstruction>>();
                    result[dominator] = dominatedNodes;
                }

                dominatedNodes.Add(dominatedNode);
            }

            return result;
        }

        private static Register NewName(Register oldVariable, IDictionary<Register, int> counters, IDictionary<Register, Stack<Register>> stacks, IList<Register> newVariables)
        {
            int index = counters[oldVariable];
            counters[oldVariable] = index + 1;

            string name = oldVariable.Name + "_" + index;

            Register result = new Register(name)
            {
                IsException = oldVariable.IsException && index == 0
            };

            Stack<Register> stack = stacks[oldVariable];
            stack.Push(result);

            newVariables.Add(result);

            return result;
        }

        private static void RenameRecursive(ControlFlowGraph cfg, LinkedListNode<TacInstruction> leader,
                                IDictionary<Register, int> counters,
                                IList<Register> newVariables,
                                IDictionary<Register, Stack<Register>> stacks,
                                IDictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> dominatorTree)
        {
            List<Register> redefinedVariables = new List<Register>();
            // Skips the label instruction, then take phis
            foreach (TacInstruction phi in cfg.BasicBlockInstructions(leader.Value).Skip(1).TakeWhile(i => i is PhiInstruction))
            {
                Register phiRes = (Register)phi.Result;
                redefinedVariables.Add(phiRes);
                Register phiNewRes = NewName(phiRes, counters, stacks, newVariables);

                phiRes.RemoveDefinition(phi);
                phiNewRes.AddDefinition(phi);
            }

            foreach (TacInstruction instruction in cfg.BasicBlockInstructions(leader.Value).Skip(1).SkipWhile(i => i is PhiInstruction))
            {
                ReplaceOperands(instruction, stacks);

                if (instruction.Result != null && instruction.Result is Register reg)
                {
                    redefinedVariables.Add(reg);
                    reg.RemoveDefinition(instruction);
                    Register newRes = NewName(reg, counters, stacks, newVariables);
                    newRes.AddDefinition(instruction);
                }
            }

            foreach (TacInstruction successor in cfg.BasicBlockSuccessors(leader.Value))
            {
                foreach (TacInstruction successorInst in cfg.BasicBlockInstructions(successor).Skip(1).TakeWhile(i => i is PhiInstruction))
                {
                    PhiInstruction phi = (PhiInstruction)successorInst;
                    // leader
                    int operandIndex = phi.Incoming.IndexOf(leader.Value);
                    Register variable = (Register)phi.Operands[operandIndex];
                    ReplaceOperand(phi, operandIndex, stacks[variable], true);
                }
            }

            if (dominatorTree.TryGetValue(leader.Value, out ISet<LinkedListNode<TacInstruction>> domSuccessors))
            {
                foreach (LinkedListNode<TacInstruction> successorNode in domSuccessors)
                {
                    RenameRecursive(cfg, successorNode, counters, newVariables, stacks, dominatorTree);
                }
            }

            foreach (Register v in redefinedVariables)
            {
                stacks[v].Pop();
            }
        }

        public static void ReplaceOperands(TacInstruction instruction, IDictionary<Register, Stack<Register>> stacks)
        {
            IList<ValueContainer> operands = instruction.Operands;
            for (int idx = 0; idx < operands.Count(); idx++)
            {
                ValueContainer v = operands[idx];
                if (v is Register reg && !reg.IsException)
                {
                    ReplaceOperand(instruction, idx, stacks[reg]);
                }
            }
        }

        private static void ReplaceOperand(TacInstruction instruction, int idx, Stack<Register> stack, bool phiOperands = false)
        {
            ValueContainer operand = instruction.Operands[idx];
            operand.RemoveUse(instruction, false);
            // UndefinedVariable is used for redundant phi nodes.
            var newOperand = phiOperands && stack.Count == 0 ? Register.UndefinedRegister : stack.Peek();
            newOperand.AddUse(instruction, false);
            instruction.Operands[idx] = newOperand;
        }
    }
}
