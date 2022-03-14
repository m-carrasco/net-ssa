﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class IRBody
    {
        public MethodBody CilBody;
        public IList<Register> Registers = new List<Register>();
        public IList<MemoryVariable> MemoryLocalVariables = new List<MemoryVariable>();
        public IList<MemoryVariable> MemoryArgumentVariables = new List<MemoryVariable>();
        public LinkedList<TacInstruction> Instructions;
        public List<ExceptionHandlerEntry> ExceptionHandlers;
        public IEnumerable<MemoryVariable> MemoryVariables
        {
            get { return MemoryLocalVariables.Union(MemoryArgumentVariables); }
        }
    }

    public class ExceptionHandlerEntry
    {
        public ExceptionHandlerEntry(ExceptionHandlerType handlerType)
        {
            HandlerType = handlerType;
        }

        public TacInstruction TryStart;
        public TacInstruction TryEnd;
        public TacInstruction FilterStart;
        public TacInstruction HandlerStart;
        public TacInstruction HandlerEnd;
        public Mono.Cecil.TypeReference CatchType;
        public ExceptionHandlerType HandlerType;
    }

    public class IntermediateRepresentation
    {
        public static IRBody Compute(MethodBody body, out IDictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>> cecilToTac)
        {
            LinkedList<TacInstruction> bytecodes = new LinkedList<TacInstruction>();

            cecilToTac = new Dictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>>();

            // This is required so there are no phi instructions at the entry.
            InsertNops(body);

            // Create one tac instruction per each cil instruction
            foreach (Mono.Cecil.Cil.Instruction cecilBytecode in body.Instructions)
            {
                var code = cecilBytecode.OpCode.Code;
                var isControlFlow = ControlFlowInstruction.IsControlFlowSequenceCode(code);

                BytecodeInstruction bytecode = isControlFlow ? new ControlFlowInstruction(cecilBytecode) : new BytecodeInstruction(cecilBytecode);

                var node = bytecodes.AddLast(bytecode);

                cecilToTac[cecilBytecode] = node;
                bytecode.Node = node;
            }

            // Set targets of net-ssa control flow instructions.
            SetControlFlowTargets(body, cecilToTac);


            return new IRBody()
            {
                CilBody = body,
                Instructions = bytecodes,
                ExceptionHandlers = GetExceptionHandlers(body, cecilToTac),
            };
        }

        private static void InsertNops(MethodBody body)
        {
            ILProcessor ilProcessor = body.GetILProcessor();
            ilProcessor.InsertBefore(body.Instructions[0], Instruction.Create(OpCodes.Nop));

            foreach (var exceptionHandler in body.ExceptionHandlers)
            {
                Instruction nop = null;
                var filterStart = exceptionHandler.FilterStart;
                if (filterStart != null)
                {
                    nop = Instruction.Create(OpCodes.Nop);
                    ilProcessor.InsertBefore(filterStart, nop);
                    exceptionHandler.FilterStart = nop;
                }

                nop = Instruction.Create(OpCodes.Nop);
                ilProcessor.InsertBefore(exceptionHandler.HandlerStart, nop);
                exceptionHandler.HandlerStart = nop;
            }

            ComputeOffsets(body);
        }

        // Taken from Mono.Cecil.Rocks, it is not public.
        private static void ComputeOffsets(MethodBody body)
        {
            var offset = 0;
            foreach (var instruction in body.Instructions)
            {
                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }
        private static void SetControlFlowTargets(MethodBody body, IDictionary<Instruction, LinkedListNode<TacInstruction>> cecilToBytecode)
        {
            foreach (Mono.Cecil.Cil.Instruction cecilBytecode in body.Instructions)
            {
                var code = cecilBytecode.OpCode.Code;
                if (!ControlFlowInstruction.IsControlFlowSequenceCode(code))
                {
                    continue;
                }

                ControlFlowInstruction controlFlowInstruction = (ControlFlowInstruction)cecilToBytecode[cecilBytecode].Value;

                var operand = cecilBytecode.Operand;
                if (operand is Instruction target)
                {
                    controlFlowInstruction.Targets.Add(cecilToBytecode[target].Value);
                }
                else if (operand is Instruction[] targets)
                {
                    controlFlowInstruction.Targets.AddRange(targets.Select(t => cecilToBytecode[t].Value));
                }
                else if (operand != null)
                {
                    throw new NotSupportedException("Unhandled case for control flow instruction: " + operand.GetType());
                }
            }
        }

        private static List<ExceptionHandlerEntry> GetExceptionHandlers(MethodBody body, IDictionary<Mono.Cecil.Cil.Instruction, LinkedListNode<TacInstruction>> cecilToTac)
        {
            List<ExceptionHandlerEntry> exceptionHandlers = new List<ExceptionHandlerEntry>();
            foreach (var cilExceptionHandler in body.ExceptionHandlers)
            {
                ExceptionHandlerEntry ourExceptionHandler = new ExceptionHandlerEntry(cilExceptionHandler.HandlerType);
                ourExceptionHandler.CatchType = cilExceptionHandler.CatchType;
                ourExceptionHandler.FilterStart = cilExceptionHandler.FilterStart != null ? cecilToTac[cilExceptionHandler.FilterStart].Value : null;
                ourExceptionHandler.HandlerEnd = cilExceptionHandler.HandlerEnd != null ? cecilToTac[cilExceptionHandler.HandlerEnd].Value : null;
                ourExceptionHandler.HandlerStart = cilExceptionHandler.HandlerStart != null ? cecilToTac[cilExceptionHandler.HandlerStart].Value : null;
                ourExceptionHandler.TryEnd = cilExceptionHandler.TryEnd != null ? cecilToTac[cilExceptionHandler.TryEnd].Value : null;
                ourExceptionHandler.TryStart = cilExceptionHandler.TryStart != null ? cecilToTac[cilExceptionHandler.TryStart].Value : null;
            }

            return exceptionHandlers;
        }
    }
}
