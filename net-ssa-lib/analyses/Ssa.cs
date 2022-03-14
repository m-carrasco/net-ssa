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

            var varDef = SsaFacts.VarDefRegisters(irBody);
            var entryInstructions = SsaFacts.EntryInstruction(method.Body);
            var successor = SsaFacts.Successor(method.Body);

            SsaQuery.Result ssaResult = SsaQuery.Query(entryInstructions, successor, varDef);

            InsertPhis(irBody, ssaResult, successor, out Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction);

            Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> successors = Successors(successor, labelToInstruction);
            List<Register> newVariables = new List<Register>();

            Rename(irBody.Instructions, irBody.Registers, newVariables, ssaResult.ImmediateDominator, labelToInstruction, successors);

            irBody.Registers = newVariables;

            RemoveUnusedPhiNodes(irBody);
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

            ISet<PhiInstruction> visited = new HashSet<PhiInstruction>();
            Stack<PhiInstruction> sources = new Stack<PhiInstruction>();

            var initials = ssaBody.Instructions.OfType<PhiInstruction>().Where(phi => phi.Result.Uses.Count == 0 || phi.Operands.Any(op => op.Equals(Register.UndefinedRegister)));
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
            }
        }

        private static void ReplacePopByNop(IRBody body)
        {
            foreach (BytecodeInstruction pop in body.Instructions.Cast<BytecodeInstruction>().Where(i => i.OpCode.Equals(OpCodes.Pop)))
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
                                    IRBody irBody,
                                    SsaQuery.Result ssaQuery,
                                    IEnumerable<(String, String)> successor,
                                    out Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction)
        {
            var instructions = irBody.Instructions;

            Dictionary<String, ISet<String>> predecessors = Predecessors(successor);
            Dictionary<String, LinkedListNode<TacInstruction>> labelToBytecode = LabelToBytecode(instructions);
            Dictionary<String, Register> nameToVariable = NameToRegister(irBody.Registers);

            uint id = 0;
            foreach ((String, String) phiLocation in ssaQuery.PhiLocation)
            {
                String variableName = phiLocation.Item1;
                String locationLabel = phiLocation.Item2;

                Register variable = nameToVariable[variableName];
                LinkedListNode<TacInstruction> locationNode = labelToBytecode[locationLabel];

                PhiInstruction phi = new PhiInstruction();
                ISet<String> predecessorLabels = predecessors[locationLabel];
                // phi.Operands = Enumerable.Repeat(variable, predecessorLabels.Count).OfType<ValueContainer>().ToList();
                // phi.Result = variable;
                foreach (var op in Enumerable.Repeat(variable, predecessorLabels.Count).OfType<ValueContainer>())
                {
                    variable.AddUse(phi);
                }
                variable.AddDefinition(phi);
                phi.Incoming = predecessorLabels.Select(t => labelToBytecode[t].Value).ToList();
                phi.Id = id++;
                var phiNode = new LinkedListNode<TacInstruction>(phi);
                phi.Node = phiNode;
                instructions.AddBefore(locationNode, phiNode);
            }

            labelToInstruction = labelToBytecode;
        }

        public static void Rename(LinkedList<TacInstruction> instructions,
                                  IList<Register> variables,
                                  IList<Register> newVariables,
                                  IEnumerable<(String, String)> imDominators,
                                  Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction,
                                  Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> successors)
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

            Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> dominatorTree = GetDominatorTree(imDominators, labelToInstruction, out List<LinkedListNode<TacInstruction>> sources);

            foreach (LinkedListNode<TacInstruction> source in sources)
            {
                Rename(source, counters, newVariables, stacks, dominatorTree, successors);
            }
        }

        private static Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> GetDominatorTree(IEnumerable<(String, String)> dominators, Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction, out List<LinkedListNode<TacInstruction>> sources)
        {
            var result = new Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>>();
            sources = new List<LinkedListNode<TacInstruction>>();

            foreach ((String dominatedLabel, String dominatorLabel) in dominators)
            {
                LinkedListNode<TacInstruction> dominatedNode = labelToInstruction[dominatedLabel];

                // Should we rather change souffle's result?
                if (dominatorLabel.Equals("ENTRY"))
                {
                    sources.Add(dominatedNode);
                    continue;
                }

                LinkedListNode<TacInstruction> dominatorNode = labelToInstruction[dominatorLabel];
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

        private static void Rename(LinkedListNode<TacInstruction> instructionNode,
                                IDictionary<Register, int> counters,
                                IList<Register> newVariables,
                                IDictionary<Register, Stack<Register>> stacks,
                                IDictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> dominatorTree,
                                IDictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> successors)
        {
            BytecodeInstruction instruction = (BytecodeInstruction)instructionNode.Value;
            List<Register> redefinedVariables = new List<Register>();

            List<PhiInstruction> phis = GetPhiInstructions(instructionNode);
            foreach (PhiInstruction phi in phis)
            {
                Register phiRes = (Register)phi.Result;
                redefinedVariables.Add(phiRes);
                Register phiNewRes = NewName(phiRes, counters, stacks, newVariables);

                phiRes.RemoveDefinition(phi);
                phiNewRes.AddDefinition(phi);
            }

            ReplaceOperands(instruction, stacks);

            if (instruction.Result != null && instruction.Result is Register reg)
            {
                redefinedVariables.Add(reg);
                reg.RemoveDefinition(instruction);
                Register newRes = NewName(reg, counters, stacks, newVariables);
                newRes.AddDefinition(instruction);
            }

            if (successors.TryGetValue(instruction, out ISet<LinkedListNode<TacInstruction>> targets))
            {
                List<PhiInstruction> successorPhis = GetSuccessorPhiInstructions(targets);

                foreach (var phi in successorPhis)
                {
                    int operandIndex = phi.Incoming.IndexOf(instruction);
                    Register variable = (Register)phi.Operands[operandIndex];
                    ReplaceOperand(phi, operandIndex, stacks[variable], true);
                }
            }

            if (dominatorTree.TryGetValue(instruction, out ISet<LinkedListNode<TacInstruction>> domSuccessors))
            {
                foreach (LinkedListNode<TacInstruction> successorNode in domSuccessors)
                {
                    Rename(successorNode, counters, newVariables, stacks, dominatorTree, successors);
                }
            }

            foreach (Register v in redefinedVariables)
            {
                stacks[v].Pop();
            }
        }

        private static List<PhiInstruction> GetSuccessorPhiInstructions(ISet<LinkedListNode<TacInstruction>> successors)
        {
            return successors.Select(successorNode => GetPhiInstructions(successorNode)).SelectMany(l => l).ToList();
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

        public static List<PhiInstruction> GetPhiInstructions(LinkedListNode<TacInstruction> instructionNode)
        {
            var current = instructionNode.Previous;
            List<PhiInstruction> phiInstructions = new List<PhiInstruction>();
            while (current != null && current.Value is PhiInstruction phi)
            {
                phiInstructions.Add(phi);
                current = current.Previous;
            }

            return phiInstructions;
        }

        public static Dictionary<String, ISet<String>> Predecessors(IEnumerable<(String, String)> edges)
        {
            Dictionary<String, ISet<String>> result = new Dictionary<String, ISet<String>>();

            foreach ((String, String) e in edges)
            {
                String source = e.Item1;
                String target = e.Item2;

                if (source.Equals("ENTRY"))
                {
                    continue;
                }

                if (!result.TryGetValue(target, out ISet<String> preds))
                {
                    preds = new HashSet<String>();
                    result[target] = preds;
                }

                preds.Add(source);
            }
            return result;
        }

        public static Dictionary<String, LinkedListNode<TacInstruction>> LabelToBytecode(LinkedList<TacInstruction> instructions)
        {
            Dictionary<String, LinkedListNode<TacInstruction>> result = new Dictionary<string, LinkedListNode<TacInstruction>>();
            LinkedListNode<TacInstruction> current = instructions.First;

            while (current != null)
            {
                result[((BytecodeInstruction)current.Value).Label()] = current;
                current = current.Next;
            }

            return result;
        }

        public static Dictionary<String, Register> NameToRegister(IEnumerable<Register> variables)
        {
            Dictionary<String, Register> result = new Dictionary<string, Register>();

            foreach (Register v in variables)
            {
                result[v.Name] = v;
            }
            return result;
        }
    }
}
