using System.Collections.Generic;
using NetSsa.Queries;
using NetSsa.Facts;
using NetSsa.Analyses;
using NetSsa.Instructions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace NetSsa.Transformations
{
    public class SsaForm
    {
        public static LinkedList<TacInstruction> InsertPhis(MethodDefinition method,
                                    LinkedList<BytecodeInstruction> instructions,
                                    List<Variable> variables,
                                    Dictionary<Instruction, List<Variable>> uses,
                                    Dictionary<Instruction, List<Variable>> definitions)
        {
            var body = method.Body;
            var varDef = SsaFacts.VarDef(definitions, uses, body.Instructions);
            var successor = SsaFacts.Successor(body);

            SsaQuery.Query(successor, varDef,
                         out IEnumerable<(String, String)> phiLocations,
                         out IEnumerable<(String, String)> dominators,
                         out IEnumerable<(String, String)> domFrontier,
                         out IEnumerable<(String, String)> edge);

            LinkedList<TacInstruction> result = new LinkedList<TacInstruction>();
            foreach (var i in instructions)
            {
                result.AddLast(i);
            }

            Dictionary<String, ISet<String>> predecessors = Predecessors(edge);
            Dictionary<String, LinkedListNode<TacInstruction>> labelToBytecode = LabelToBytecode(result);
            Dictionary<String, Variable> nameToVariable = NameToVariable(variables);

            uint id = 0;
            foreach ((String, String) phiLocation in phiLocations)
            {
                String variableName = phiLocation.Item1;
                String locationLabel = phiLocation.Item2;

                Variable variable = nameToVariable[variableName];
                LinkedListNode<TacInstruction> locationNode = labelToBytecode[locationLabel];

                PhiInstruction phi = new PhiInstruction();
                phi.Operands = Enumerable.Repeat(variable, predecessors.Count).ToList();
                phi.Result = variable;
                ISet<String> predecessorLabels = predecessors[locationLabel];
                phi.Incoming = predecessorLabels.Select(t => labelToBytecode[t].Value).ToList();
                phi.Id = id++;
                result.AddBefore(locationNode, new LinkedListNode<TacInstruction>(phi));
            }

            return result;
        }

        public static Dictionary<String, ISet<String>> Predecessors(IEnumerable<(String, String)> edges)
        {
            Dictionary<String, ISet<String>> result = new Dictionary<String, ISet<String>>();

            foreach ((String, String) e in edges)
            {
                String source = e.Item1;
                String target = e.Item2;

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