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
    public class SsaBody
    {
        public MethodBody CilBody;
        public List<Variable> Variables;

        public List<Variable> Arguments
        {
            get { return Variables.Where(v => v.IsArgumentVariable()).ToList(); }
        }

        public LinkedList<TacInstruction> Instructions;

        public Dictionary<Variable, ISet<LinkedListNode<TacInstruction>>> Users;

        public Dictionary<Variable, LinkedListNode<TacInstruction>> Definitions;

        public IEnumerable<TacInstruction> Entries()
        {
            yield return Instructions.First();

            var cilExceptionalEntries = new HashSet<Mono.Cecil.Cil.Instruction>();
            foreach (var exceptionHandler in CilBody.ExceptionHandlers)
            {
                var filterStart = exceptionHandler.FilterStart;
                if (filterStart != null)
                {
                    cilExceptionalEntries.Add(filterStart);
                }

                cilExceptionalEntries.Add(exceptionHandler.HandlerStart);
            }

            foreach (var inst in Instructions)
            {
                if (inst is BytecodeInstruction bytecode)
                {
                    if (cilExceptionalEntries.Contains(bytecode.Bytecode))
                        yield return inst;
                }
            }
        }
    }

    public class Ssa
    {
        public static SsaBody Compute(MethodDefinition method, BytecodeBody bytecodeBody)
        {
            var varDef = SsaFacts.VarDef(bytecodeBody.Instructions);
            var entryInstructions = SsaFacts.EntryInstruction(method.Body);
            var successor = SsaFacts.Successor(method.Body);

            SsaQuery.Result ssaResult = SsaQuery.Query(entryInstructions, successor, varDef);

            LinkedList<TacInstruction> ssaInstructions = InsertPhis(bytecodeBody, ssaResult, successor, out Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction);

            Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> successors = Successors(successor, labelToInstruction);
            List<Variable> newVariables = new List<Variable>();
            Rename(ssaInstructions, bytecodeBody.Variables, newVariables, ssaResult.ImmediateDominator, labelToInstruction, successors);

            // Map a variable to the unique instruction that defines it
            Dictionary<Variable, LinkedListNode<TacInstruction>> definitions = new Dictionary<Variable, LinkedListNode<TacInstruction>>();
            // Map a variable to every instruction that uses it as an operand
            Dictionary<Variable, ISet<LinkedListNode<TacInstruction>>> users = new Dictionary<Variable, ISet<LinkedListNode<TacInstruction>>>();
            foreach (Variable v in newVariables)
            {
                users[v] = new HashSet<LinkedListNode<TacInstruction>>();
            }
            users[Variable.UndefinedVariable] = new HashSet<LinkedListNode<TacInstruction>>();

            CalculateDefinitionsAndUsers(ssaInstructions, definitions, users);

            // An unused phi node may have as an operand another phi node which is only used by the removed phi.
            int lastSize;
            do
            {
                lastSize = ssaInstructions.Count;
                RemoveUnusedPhiNodes(ssaInstructions, definitions, users, newVariables);
            } while (lastSize != ssaInstructions.Count);

            var ssaBody = new SsaBody()
            {
                CilBody = method.Body,
                Instructions = ssaInstructions,
                Definitions = definitions,
                Users = users,
                Variables = newVariables
            };

            return ssaBody;
        }

        private static void RemoveUnusedPhiNodes(LinkedList<TacInstruction> ssaInstructions, Dictionary<Variable, LinkedListNode<TacInstruction>> definitions, Dictionary<Variable, ISet<LinkedListNode<TacInstruction>>> users, List<Variable> newVariables)
        {
            LinkedListNode<TacInstruction> instructionNode = ssaInstructions.First;

            while (instructionNode != null)
            {
                LinkedListNode<TacInstruction> next = instructionNode.Next;

                if (instructionNode.Value is PhiInstruction phiInstruction)
                {
                    Variable phiResult = phiInstruction.Result;
                    bool isUnused = users[phiResult].Count() == 0;
                    if (isUnused || UnfeasiblePhiNode(phiInstruction, definitions))
                    {
                        definitions.Remove(phiResult);
                        foreach (var operand in phiInstruction.Operands)
                        {
                            users[operand].Remove(instructionNode);
                        }
                        ssaInstructions.Remove(instructionNode);
                        newVariables.Remove(phiResult);
                    }
                }

                instructionNode = next;
            }
        }

        private static bool UnfeasiblePhiNode(PhiInstruction phi, Dictionary<Variable, LinkedListNode<TacInstruction>> definitions)
        {
            // This is generally the case for phi nodes just consuming each other.
            return phi.Operands.Any(operand => Variable.UndefinedVariable.Equals(operand) || !definitions.ContainsKey(operand) || definitions[operand].Value is PhiInstruction);
        }

        private static void CalculateDefinitionsAndUsers(LinkedList<TacInstruction> ssaInstructions, Dictionary<Variable, LinkedListNode<TacInstruction>> definitions, Dictionary<Variable, ISet<LinkedListNode<TacInstruction>>> users)
        {
            LinkedListNode<TacInstruction> instructionNode = ssaInstructions.First;
            while (instructionNode != null)
            {
                TacInstruction instruction = instructionNode.Value;
                Variable resultVariable = instruction.Result;
                if (resultVariable != null)
                {
                    definitions[resultVariable] = instructionNode;
                }

                foreach (Variable usedVariable in instruction.Operands)
                {
                    users[usedVariable].Add(instructionNode);
                }

                instructionNode = instructionNode.Next;
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

        public static LinkedList<TacInstruction> InsertPhis(
                                    BytecodeBody bytecodeBody,
                                    SsaQuery.Result ssaQuery,
                                    IEnumerable<(String, String)> successor,
                                    out Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction)
        {
            LinkedList<TacInstruction> instructions = bytecodeBody.Instructions;

            LinkedList<TacInstruction> result = new LinkedList<TacInstruction>();
            foreach (var i in instructions)
            {
                i.Node = result.AddLast(i);
            }

            Dictionary<String, ISet<String>> predecessors = Predecessors(successor);
            Dictionary<String, LinkedListNode<TacInstruction>> labelToBytecode = LabelToBytecode(result);
            Dictionary<String, Variable> nameToVariable = NameToVariable(bytecodeBody.Variables);

            uint id = 0;
            foreach ((String, String) phiLocation in ssaQuery.PhiLocation)
            {
                String variableName = phiLocation.Item1;
                String locationLabel = phiLocation.Item2;

                Variable variable = nameToVariable[variableName];
                LinkedListNode<TacInstruction> locationNode = labelToBytecode[locationLabel];

                PhiInstruction phi = new PhiInstruction();
                ISet<String> predecessorLabels = predecessors[locationLabel];
                phi.Operands = Enumerable.Repeat(variable, predecessorLabels.Count).ToList();
                phi.Result = variable;
                phi.Incoming = predecessorLabels.Select(t => labelToBytecode[t].Value).ToList();
                phi.Id = id++;
                var phiNode = new LinkedListNode<TacInstruction>(phi);
                phi.Node = phiNode;
                result.AddBefore(locationNode, phiNode);
            }

            labelToInstruction = labelToBytecode;

            return result;
        }

        public static void Rename(LinkedList<TacInstruction> instructions,
                                  List<Variable> variables,
                                  List<Variable> newVariables,
                                  IEnumerable<(String, String)> imDominators,
                                  Dictionary<String, LinkedListNode<TacInstruction>> labelToInstruction,
                                  Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> successors)
        {
            // The key is original variables
            Dictionary<Variable, int> counters = new Dictionary<Variable, int>();

            // The key is from the orignal set of variables
            // Variables in the stacks are the new ones.
            Dictionary<Variable, Stack<Variable>> stacks = new Dictionary<Variable, Stack<Variable>>();

            foreach (var v in variables)
            {
                if (!v.IsStackVariable())
                {
                    newVariables.Add(v);
                    continue;
                }

                counters[v] = 0;
                stacks[v] = new Stack<Variable>();
            }

            Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> dominatorTree = GetDominatorTree(imDominators, labelToInstruction, out List<LinkedListNode<TacInstruction>> sources);

            LinkedListNode<TacInstruction> first = instructions.First;

            foreach (var source in sources)
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

        private static Variable NewName(Variable oldVariable, Dictionary<Variable, int> counters, Dictionary<Variable, Stack<Variable>> stacks, List<Variable> newVariables)
        {
            int index = counters[oldVariable];
            counters[oldVariable] = index + 1;

            string name = oldVariable.Name + "_" + index;

            Variable result = new Variable(name);
            Stack<Variable> stack = stacks[oldVariable];
            stack.Push(result);

            newVariables.Add(result);

            return result;
        }

        private static void Rename(LinkedListNode<TacInstruction> instructionNode,
                                Dictionary<Variable, int> counters,
                                List<Variable> newVariables,
                                Dictionary<Variable, Stack<Variable>> stacks,
                                Dictionary<TacInstruction, ISet<LinkedListNode<TacInstruction>>> dominatorTree,
                                Dictionary<BytecodeInstruction, ISet<LinkedListNode<TacInstruction>>> successors)
        {
            BytecodeInstruction instruction = (BytecodeInstruction)instructionNode.Value;
            List<Variable> redefinedVariables = new List<Variable>();

            List<PhiInstruction> phis = GetPhiInstructions(instructionNode);
            foreach (PhiInstruction phi in phis)
            {
                redefinedVariables.Add(phi.Result);
                phi.Result = NewName(phi.Result, counters, stacks, newVariables);
            }

            ReplaceOperands(instruction.Operands, stacks);

            if (instruction.Result != null)
            {
                redefinedVariables.Add(instruction.Result);
                instruction.Result = NewName(instruction.Result, counters, stacks, newVariables);
            }

            if (successors.TryGetValue(instruction, out ISet<LinkedListNode<TacInstruction>> targets))
            {
                List<PhiInstruction> successorPhis = GetSuccessorPhiInstructions(targets);

                foreach (var phi in successorPhis)
                {
                    int operandIndex = phi.Incoming.IndexOf(instruction);
                    Variable variable = phi.Operands[operandIndex];
                    ReplaceOperand(phi.Operands, operandIndex, stacks[variable], true);
                }
            }

            if (dominatorTree.TryGetValue(instruction, out ISet<LinkedListNode<TacInstruction>> domSuccessors))
            {
                foreach (LinkedListNode<TacInstruction> successorNode in domSuccessors)
                {
                    Rename(successorNode, counters, newVariables, stacks, dominatorTree, successors);
                }
            }

            foreach (Variable v in redefinedVariables)
            {
                stacks[v].Pop();
            }
        }

        private static List<PhiInstruction> GetSuccessorPhiInstructions(ISet<LinkedListNode<TacInstruction>> successors)
        {
            return successors.Select(successorNode => GetPhiInstructions(successorNode)).SelectMany(l => l).ToList();
        }

        public static void ReplaceOperands(List<Variable> variables, Dictionary<Variable, Stack<Variable>> stacks)
        {
            for (int idx = 0; idx < variables.Count(); idx++)
            {
                Variable v = variables[idx];
                if (v.IsStackVariable())
                {
                    ReplaceOperand(variables, idx, stacks[v]);
                }
            }
        }

        private static void ReplaceOperand(List<Variable> variables, int idx, Stack<Variable> stack, bool phiOperands = false)
        {
            // UndefinedVariable is used for redundant phi nodes.
            variables[idx] = phiOperands && stack.Count == 0 ? Variable.UndefinedVariable : stack.Peek();
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

        public static Dictionary<String, Variable> NameToVariable(List<Variable> variables)
        {
            Dictionary<String, Variable> result = new Dictionary<string, Variable>();

            foreach (Variable v in variables)
            {
                result[v.Name] = v;
            }
            return result;
        }
    }
}
