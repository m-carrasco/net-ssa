using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class Unstacker
    {
        public static IRBody Compute(MethodBody body)
        {
            return Compute(body, out IDictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>> cecilToTac);
        }

        public static IRBody Compute(MethodBody body, out IDictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>> cecilToTac)
        {
            IRBody irBody = IntermediateRepresentation.Compute(body, out cecilToTac);

            int max_stack;
            var stack_sizes = ComputeStackSize(body, out max_stack);

            for (uint index = 0; index < max_stack; index++)
            {
                irBody.Registers.Add(new Register((uint)index));
            }

            for (uint index = 0; index < body.Variables.Count; index++)
            {
                irBody.MemoryLocalVariables.Add(new MemoryVariable(MemoryVariable.LocalVariablePrefix + index, MemoryVariableKind.Local));
            }

            if (body.Method.HasThis)
            {
                irBody.MemoryArgumentVariables.Add(new MemoryVariable(MemoryVariable.ArgumentVariablePrefix + "0", MemoryVariableKind.Argument));
            }

            int offset = body.Method.HasThis ? 1 : 0;
            for (uint index = 0; index < body.Method.Parameters.Count; index++)
            {
                irBody.MemoryArgumentVariables.Add(new MemoryVariable(MemoryVariable.ArgumentVariablePrefix + (index + offset), MemoryVariableKind.Argument));
            }

            foreach (var kv in stack_sizes)
            {
                Instruction instruction = kv.Key;
                int stack_size = kv.Value;

                // Analyze which stack slots are consumed
                int popDelta = ComputePopDelta(instruction, IsReturnTypeVoid(body), stack_size);

                TacInstruction tacInstruction = cecilToTac[instruction].Value;
                foreach (var index in Enumerable.Range(stack_size - popDelta, popDelta))
                {
                    Register usedOperand = irBody.Registers[index];
                    usedOperand.AddUse(tacInstruction);
                }

                int pushDelta = ComputePushDelta(instruction);
                // for sake of simplicity, a DUP opcode only defines
                // one variable. semantic-wise, the consumed operand is left
                // untouched.
                int skip = instruction.OpCode.Code == OpCodes.Dup.Code ? 1 : 0;
                foreach (var index in Enumerable.Range(stack_size - popDelta, pushDelta).Skip(skip))
                {
                    Register definedRegister = irBody.Registers[index];
                    definedRegister.AddDefinition(tacInstruction);
                }

                // Analyze which local variables or arguments are consumed
                ConsumeLocalVariables(instruction, tacInstruction, irBody, body.Method.HasThis);
            }

            SetExceptionVariables(body, irBody, cecilToTac);

            return irBody;
        }

        private static Dictionary<Code, int> VariableIndex = new Dictionary<Code, int>()
        {
            { Code.Ldloc_0, 0}, { Code.Ldloc_1, 1}, { Code.Ldloc_2, 2}, { Code.Ldloc_3, 3},
            { Code.Ldarg_0, 0}, { Code.Ldarg_1, 1}, { Code.Ldarg_2, 2}, { Code.Ldarg_3, 3},
            { Code.Stloc_0, 0}, { Code.Stloc_1, 1}, { Code.Stloc_2, 2}, { Code.Stloc_3, 3},
        };

        static void ConsumeLocalVariables(Instruction instruction, TacInstruction tacInstruction, IRBody irBody, bool hasThis)
        {
            var code = instruction.OpCode.Code;
            switch (code)
            {
                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                    {
                        MemoryVariable var = irBody.MemoryLocalVariables[VariableIndex[code]];
                        var.AddUse(tacInstruction);
                        break;
                    }
                case Code.Ldloca_S:
                case Code.Ldloca:
                case Code.Ldloc:
                case Code.Ldloc_S:
                    {
                        VariableDefinition variableDefinition = (VariableDefinition)instruction.Operand;
                        MemoryVariable var = irBody.MemoryLocalVariables[variableDefinition.Index];
                        var.AddUse(tacInstruction); ;
                        break;
                    }
                case Code.Stloc_3:
                case Code.Stloc_2:
                case Code.Stloc_1:
                case Code.Stloc_0:
                    {
                        MemoryVariable var = irBody.MemoryLocalVariables[VariableIndex[code]];
                        var.AddDefinition(tacInstruction);
                        break;
                    }
                case Code.Stloc:
                case Code.Stloc_S:
                    {
                        var variableDefinition = (VariableDefinition)instruction.Operand;
                        MemoryVariable var = irBody.MemoryLocalVariables[variableDefinition.Index];
                        var.AddDefinition(tacInstruction);
                        break;
                    }
                case Code.Starg:
                case Code.Starg_S:
                    {
                        var parameterDefinition = (ParameterDefinition)instruction.Operand;
                        MemoryVariable var = irBody.MemoryArgumentVariables[parameterDefinition.Index];
                        var.AddDefinition(tacInstruction);
                        break;
                    }
                case Code.Ldarg_0:
                case Code.Ldarg_1:
                case Code.Ldarg_2:
                case Code.Ldarg_3:
                    {
                        MemoryVariable var = irBody.MemoryArgumentVariables[VariableIndex[code]];
                        var.AddUse(tacInstruction);
                        break;
                    }
                case Code.Ldarg:
                case Code.Ldarg_S:
                case Code.Ldarga_S:
                case Code.Ldarga:
                    {
                        var parameterDefinition = (ParameterDefinition)instruction.Operand;
                        int index = parameterDefinition.Index;
                        int offset = hasThis ? 1 : 0;
                        MemoryVariable var = irBody.MemoryArgumentVariables[index + offset];
                        var.AddUse(tacInstruction);
                        break;
                    }
            }
        }
        static void SetExceptionVariables(MethodBody body, IRBody irBody, IDictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>> cecilToTac)
        {
            /*
                At the beginning of a catch handler, the stack always contains the caught exception.
                This stack slot is not defined by an instruction in the method body. The assignment is performed
                by the exception handling mechanism of the runtime.

                At bytecode level, suppose we have:

                    void foo() {
                        try {
                            ldc_1
                            new ...
                            throw
                        } catch {
                            // the stack has size 1 and contains
                            // a reference to the caught exception
                            pop
                            leave ...
                        }
                    }

                    If we do not assign a specific variable, then we would have:

                    try {
                        s0 = 1
                        s1 = new ....
                        throw s1
                    } catch{
                        pop [s0]
                        leave ...
                    }

                    which is not accurate because pop would never consume 's0 = 1'.
                    The objective is to generate:

                    try {
                        s0 = 1
                        s1 = new ...
                        throw s1
                    } catch{
                        pop [e0]
                        leave ...
                    }

                    where 'e0' is the representing the caught exception loaded by the runtime.
            */

            if (!body.HasExceptionHandlers)
            {
                return;
            }

            Register stackSlotZero = irBody.Registers[0];

            int exceptionIndex = 0;
            foreach (var handler in body.ExceptionHandlers)
            {
                if (handler.HandlerType == ExceptionHandlerType.Fault)
                {
                    continue;
                }

                if (handler.HandlerType != ExceptionHandlerType.Filter &&
                    handler.FilterStart != null)
                {
                    // It is assumed this cannot happen.
                    throw new NotImplementedException("Unexpected exception handler with non-null FilterStart.");
                }

                if (handler.HandlerType == ExceptionHandlerType.Catch ||
                    handler.HandlerType == ExceptionHandlerType.Filter)
                {
                    SetExceptionVariable(body, stackSlotZero, handler, cecilToTac, irBody.Registers, ref exceptionIndex);
                }
            }
        }

        static void SetExceptionVariable(MethodBody body, Register stackSlotZero, ExceptionHandler handler, IDictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>> cecilToTac, IList<Register> registers, ref int exceptionIndex)
        {
            if (handler.FilterStart != null)
            {
                ChangeVariableFirstUse(handler.FilterStart, handler.HandlerStart, stackSlotZero, cecilToTac, registers, ref exceptionIndex);
            }

            ChangeVariableFirstUse(handler.HandlerStart, handler.HandlerEnd, stackSlotZero, cecilToTac, registers, ref exceptionIndex);
        }

        static void ChangeVariableFirstUse(Instruction start, Instruction end, Register stackSlotZero, IDictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>> cecilToTac, IList<Register> registers, ref int exceptionIndex)
        {
            var currentInstruction = start;
            while (currentInstruction != end)
            {
                TacInstruction tacInstruction = cecilToTac[currentInstruction].Value;

                if (stackSlotZero.Uses.Contains(tacInstruction))
                {

                    Register exceptionRegister = new Register(Register.ExceptionPrefix + (exceptionIndex++))
                    {
                        IsException = true
                    };

                    registers.Add(exceptionRegister);

                    exceptionRegister.Uses.Add(tacInstruction);
                    int index = tacInstruction.Operands.IndexOf(stackSlotZero);
                    tacInstruction.Operands[index] = exceptionRegister;
                    stackSlotZero.RemoveUse(tacInstruction, false);
                    break;
                }

                currentInstruction = currentInstruction.Next;
            }
        }

        static bool IsReturnTypeVoid(MethodBody body)
        {
            return body.Method.ReturnType.Equals(body.Method.Module.TypeSystem.Void);
        }

        static Dictionary<Instruction, int> ComputeStackSize(MethodBody body, out int max_stack)
        {
            Dictionary<Instruction, int> stack_sizes = new Dictionary<Instruction, int>();
            int stack_size = 0;
            max_stack = 0;

            if (body.HasExceptionHandlers)
                ComputeExceptionHandlerStackSize(body, stack_sizes);

            bool returnVoid = IsReturnTypeVoid(body);

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
                ComputeStackDelta(instruction, ref stack_size, returnVoid);
                max_stack = System.Math.Max(max_stack, stack_size);

                CopyBranchStackSize(instruction, stack_sizes, stack_size);
                ComputeStackSize(instruction, ref stack_size);
            }

            return stack_sizes;
        }

        static void ComputeExceptionHandlerStackSize(MethodBody body, Dictionary<Instruction, int> stack_sizes)
        {
            var exception_handlers = body.ExceptionHandlers;

            for (int i = 0; i < exception_handlers.Count; i++)
            {
                var exception_handler = exception_handlers[i];

                switch (exception_handler.HandlerType)
                {
                    case ExceptionHandlerType.Catch:
                        AddExceptionStackSize(exception_handler.HandlerStart, stack_sizes);
                        break;
                    case ExceptionHandlerType.Filter:
                        AddExceptionStackSize(exception_handler.FilterStart, stack_sizes);
                        AddExceptionStackSize(exception_handler.HandlerStart, stack_sizes);
                        break;
                }
            }
        }

        static void AddExceptionStackSize(Instruction handler_start, Dictionary<Instruction, int> stack_sizes)
        {
            if (handler_start == null)
                return;

            stack_sizes[handler_start] = 1;
        }

        static void ComputeStackDelta(Instruction instruction, ref int stack_size, bool returnVoid)
        {
            int popDelta = ComputePopDelta(instruction, returnVoid, stack_size);
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

        static int ComputePopDelta(Instruction instruction, bool returnVoid, int stack_size)
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
                case FlowControl.Return:
                    if (instruction.OpCode.Code == OpCodes.Endfinally.Code ||
                        instruction.OpCode.Code == OpCodes.Endfilter.Code)
                    {
                        return ComputePopDelta(instruction.OpCode.StackBehaviourPop, stack_size);
                    }
                    return returnVoid ? 0 : 1;
                default:
                    return ComputePopDelta(instruction.OpCode.StackBehaviourPop, stack_size);
            }
        }
        static int ComputePopDelta(StackBehaviour pop_behavior, int stack_size)
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
                    return stack_size;
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
