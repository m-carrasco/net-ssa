using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace NetSsa.Analyses
{
    public class Edges
    {
        public static void NonExceptionalEdges(IDictionary<Instruction, ISet<Instruction>> edges, MethodBody methodBody)
        {
            foreach (Instruction instruction in methodBody.Instructions)
            {
                if (!edges.TryGetValue(instruction, out ISet<Instruction> successors))
                {
                    successors = new HashSet<Instruction>();
                    edges[instruction] = successors;
                }

                FlowControl flowControl = instruction.OpCode.FlowControl;
                bool hasNext = true;
                switch (instruction.OpCode.FlowControl)
                {
                    case FlowControl.Next:
                        break;
                    case FlowControl.Meta:
                        break;
                    case FlowControl.Cond_Branch:
                        if (instruction.Operand is Instruction[] targets)
                        {
                            foreach (var t in targets)
                            {
                                successors.Add(t);
                            }
                        }
                        else
                        {
                            successors.Add((Instruction)instruction.Operand);
                        }
                        break;
                    case FlowControl.Branch:
                        hasNext = false;
                        successors.Add((Instruction)instruction.Operand);
                        break;
                    case FlowControl.Return:
                    case FlowControl.Throw:
                        hasNext = false;
                        break;
                    case FlowControl.Call:
                        hasNext = instruction.OpCode.Code != Code.Jmp;
                        break;
                    default:
                        throw new NotImplementedException("Unhandled flow control type: " + flowControl);
                }

                if (hasNext && instruction.Next != null)
                {
                    successors.Add(instruction.Next);
                }
            }
        }

        public static void ExceptionalEdges(IDictionary<Instruction, ISet<Instruction>> edges, MethodBody methodBody)
        {
            if (!methodBody.HasExceptionHandlers)
                return;

            foreach (ExceptionHandler handler in methodBody.ExceptionHandlers)
            {
                var current = handler.TryStart;
                var isFilter = handler.HandlerType == ExceptionHandlerType.Filter;
                do
                {
                    if (!edges.TryGetValue(current, out ISet<Instruction> successors))
                    {
                        successors = new HashSet<Instruction>();
                        edges[current] = successors;
                    }

                    successors.Add(handler.HandlerStart);

                    if (isFilter)
                        successors.Add(handler.FilterStart);

                    current = current.Next;
                } while (current != handler.TryEnd);


                if (handler.HandlerType == ExceptionHandlerType.Finally)
                {
                    current = handler.HandlerStart;
                    do
                    {
                        if (current.OpCode == OpCodes.Endfinally)
                        {
                            if (!edges.TryGetValue(current, out ISet<Instruction> successors))
                            {
                                successors = new HashSet<Instruction>();
                                edges[current] = successors;
                            }

                            successors.Add(handler.HandlerEnd);
                        }

                        current = current.Next;
                    } while (current != handler.HandlerEnd);
                }
            }
        }
    }
}