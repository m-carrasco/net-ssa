using System.Collections.Generic;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using Mono.Cecil;

namespace NetSsa.Analyses
{
    public class VariableDefUse
    {
        public static void Compute(MethodBody body, Dictionary<Instruction, List<Variable>> uses, Dictionary<Instruction, List<Variable>> defs)
        {
            if (!body.Method.IsStatic)
                throw new NotImplementedException("check args and local vars");

            int max_stack;
            var stack_sizes = ComputeStackSize(body, out max_stack);

            var stackVariables = Enumerable.Range(0, max_stack).Select(index => new Variable("s" + index)).ToList();
            var localVariables = Enumerable.Range(0, body.Variables.Count).Select(index => new Variable("l" + index)).ToList();
            var argVariables = Enumerable.Range(0, body.Method.Parameters.Count).Select(index => new Variable("a" + index)).ToList();

            foreach (var kv in stack_sizes)
            {
                Instruction instruction = kv.Key;
                int stack_size = kv.Value;

                int popDelta = ComputePopDelta(instruction);
                uses[instruction] = Enumerable.Range(stack_size - popDelta, popDelta).Select(index => stackVariables[index]).ToList();

                int pushDelta = ComputePushDelta(instruction);
                defs[instruction] = Enumerable.Range(stack_size - popDelta, pushDelta).Select(index => stackVariables[index]).ToList();

                switch (instruction.OpCode.Code)
                {
                    case Code.Nop:
                    case Code.Ldc_I4:
                    case Code.Ldc_I4_0:
                    case Code.Ldc_I4_1:
                    case Code.Cgt:
                    case Code.Brfalse_S:
                    case Code.Br_S:
                    case Code.Ble_S:
                    case Code.Ret:
                        break;
                    case Code.Ldarg_0:
                        uses[instruction].Add(argVariables[0]);
                        break;
                    case Code.Stloc_2:
                        defs[instruction].Add(localVariables[2]);
                        break;
                    case Code.Stloc_1:
                        defs[instruction].Add(localVariables[1]);
                        break;
                    case Code.Stloc_0:
                        defs[instruction].Add(localVariables[0]);
                        break;
                    case Code.Ldloc_0:
                        uses[instruction].Add(localVariables[0]);
                        break;
                    case Code.Ldloc_1:
                        uses[instruction].Add(localVariables[1]);
                        break;
                    case Code.Ldloc_2:
                        uses[instruction].Add(localVariables[2]);
                        break;
                    default:
                        throw new NotImplementedException("Unhandled instruction: " + instruction);
                }
            }
        }


        static Dictionary<Instruction, int> ComputeStackSize(MethodBody body, out int max_stack)
        {
            Dictionary<Instruction, int> stack_sizes = new Dictionary<Instruction, int>();
            int stack_size = 0;
            max_stack = 0;

            foreach (var instruction in body.Instructions)
            {
                int computed_size;
                // If the instruction is the target of a control flow instruction
                // resume with the stack_size calculated there
                // Otherwise, continue with the value calculated from the previous one.
                if (stack_sizes.TryGetValue(instruction, out computed_size))
                    stack_size = computed_size;
                else
                    stack_sizes[instruction] = stack_size;

                max_stack = System.Math.Max(max_stack, stack_size);
                ComputeStackDelta(instruction, ref stack_size);
                max_stack = System.Math.Max(max_stack, stack_size);

                CopyBranchStackSize(instruction, stack_sizes, stack_size);
                ComputeStackSize(instruction, ref stack_size);
            }

            return stack_sizes;
        }

        static void ComputeStackDelta(Instruction instruction, ref int stack_size)
        {
            int popDelta = ComputePopDelta(instruction);
            stack_size -= popDelta;
            int pushDelta = ComputePushDelta(instruction);
            stack_size += pushDelta;
        }

        static void CopyBranchStackSize(Instruction instruction, Dictionary<Instruction, int> stack_sizes, int stack_size)
        {
            if (stack_size == 0)
                return;

            switch (instruction.OpCode.OperandType)
            {
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    CopyBranchStackSize(stack_sizes, (Instruction)instruction.Operand, stack_size);
                    break;
                case OperandType.InlineSwitch:
                    var targets = (Instruction[])instruction.Operand;
                    for (int i = 0; i < targets.Length; i++)
                        CopyBranchStackSize(stack_sizes, targets[i], stack_size);
                    break;
            }
        }

        static void CopyBranchStackSize(Dictionary<Instruction, int> stack_sizes, Instruction target, int stack_size)
        {
            int branch_stack_size = stack_size;

            int computed_size;
            if (stack_sizes.TryGetValue(target, out computed_size))
            {
                // We should actually throw an exception if there is a mistmatch between
                // computed_size and stack_size. We are assuming that the input bytecode is legal.
                branch_stack_size = System.Math.Max(branch_stack_size, computed_size);
            }

            stack_sizes[target] = branch_stack_size;
        }

        static void ComputeStackSize(Instruction instruction, ref int stack_size)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case FlowControl.Branch:
                case FlowControl.Throw:
                case FlowControl.Return:
                    stack_size = 0;
                    break;
            }
        }

        static int ComputePopDelta(Instruction instruction)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case FlowControl.Call:
                    {
                        int delta = 0;

                        var method = (IMethodSignature)instruction.Operand;
                        // pop 'this' argument
                        if (method.HasThis && !method.ExplicitThis && instruction.OpCode.Code != Code.Newobj)
                            delta++;
                        // pop normal arguments
                        if (method.HasParameters)
                            delta += method.Parameters.Count;
                        // pop function pointer
                        if (instruction.OpCode.Code == Code.Calli)
                            delta++;
                        return delta;
                    }
                default:
                    return ComputePopDelta(instruction.OpCode.StackBehaviourPop);
            }
        }
        static int ComputePopDelta(StackBehaviour pop_behavior)
        {
            switch (pop_behavior)
            {
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                case StackBehaviour.Pop1:
                    return 1;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    return 2;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    return 3;
                case StackBehaviour.PopAll:
                    return 0;
                default:
                    return 0;
            }


        }

        static int ComputePushDelta(Instruction instruction)
        {
            switch (instruction.OpCode.FlowControl)
            {
                case FlowControl.Call:
                    {
                        int delta = 0;
                        var method = (IMethodSignature)instruction.Operand;
                        // push return value
                        var returnType = method.ReturnType;
                        if (!returnType.Equals(returnType.Module.TypeSystem.Void) || instruction.OpCode.Code == Code.Newobj)
                            delta++;
                        return delta;
                    }
                default:
                    return ComputePushDelta(instruction.OpCode.StackBehaviourPush);
            }
        }
        static int ComputePushDelta(StackBehaviour push_behaviour)
        {
            switch (push_behaviour)
            {
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    return 1;
                case StackBehaviour.Push1_push1:
                    return 2;
                default:
                    return 0;
            }
        }

    }
}