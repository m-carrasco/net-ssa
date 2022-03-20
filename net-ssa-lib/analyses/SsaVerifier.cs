using System.Collections.Generic;
using NetSsa.Instructions;
using System;
using System.Linq;

namespace NetSsa.Analyses
{
    // These checks are inspired by the ones performed in the LLVM project
    public class SsaVerifier
    {
        private IRBody _ssaBody { get; init; }

        private ControlFlowGraph _cfg;

        private Dominance _dom;
        public SsaVerifier(IRBody ssaBody)
        {
            _ssaBody = ssaBody;
            _cfg = new ControlFlowGraph(_ssaBody);
            _dom = new Dominance(_cfg);
        }

        public void Verify()
        {
            ISet<Register> defined = new HashSet<Register>();
            foreach (TacInstruction instruction in _ssaBody.Instructions)
            {
                CheckSelfReferential(instruction);
                CheckOnlyOneDefinition(instruction, defined);
                CheckUsesDominatedByDefinition(instruction);
            }

            foreach (TacInstruction leader in _cfg.Leaders())
            {
                CheckPhiNodesInBasicBlock(leader);
            }
        }

        private void CheckOnlyOneDefinition(TacInstruction instruction, ISet<Register> defined)
        {
            if (instruction.Result != null && instruction.Result is Register regResult)
            {
                if (defined.Contains(regResult))
                {
                    throw new VerifierException("Register " + regResult.Name + " is defined twice.");
                }

                defined.Add(regResult);
            }
        }

        private void CheckUsesDominatedByDefinition(TacInstruction instruction)
        {
            foreach (Register operand in instruction.Operands.OfType<Register>())
            {
                if (operand.IsException)
                    continue;

                // The operand must be dominated by its definition.
                TacInstruction definingInstruction = operand.Definitions.Single();

                TacInstruction leaderDefining = _cfg.GetLeader(definingInstruction);
                ISet<TacInstruction> dom = _dom.Dom(leaderDefining);

                TacInstruction instructionLeader = null;

                if (instruction is PhiInstruction phi)
                {
                    int index = phi.Operands.IndexOf(operand);
                    TacInstruction predecessor = phi.Incoming[index];
                    instructionLeader = _cfg.GetLeader(predecessor);
                }
                else
                {
                    instructionLeader = _cfg.GetLeader(instruction);
                }

                if (!dom.Contains(instructionLeader))
                {
                    throw new VerifierException("Operand " + operand.Name + " of instruction " + instruction.ToString() + " is not dominated by " + definingInstruction.ToString());
                }

                if (instructionLeader == leaderDefining)
                {
                    // TODO: Do extra check in case they are in the same basic block.
                }
            }
        }

        private void CheckSelfReferential(TacInstruction instruction)
        {
            if (instruction is PhiInstruction)
                return;

            ValueContainer result = instruction.Result;

            if (instruction.Operands.Contains(result))
            {
                throw new VerifierException("Instruction " + instruction.ToString() + " is self referential.");
            }
        }

        private void CheckPhiNodesInBasicBlock(TacInstruction leader)
        {
            bool canBePhiNode = true;
            // Skip label
            foreach (TacInstruction bbInstruction in _cfg.BasicBlockInstructions(leader).Skip(1))
            {
                if (bbInstruction is PhiInstruction phi)
                {
                    if (!canBePhiNode)
                    {
                        throw new VerifierException("PHIs must be the first thing in a basic block, all grouped together: " + phi.ToString());
                    }

                    TacInstruction phiLeader = _cfg.GetLeader(phi);
                    IEnumerable<TacInstruction> predecessors = _cfg.BasicBlockPredecessors(phiLeader);

                    if (phi.Operands.Count != predecessors.Count())
                    {
                        throw new VerifierException("Phi instruction with a different amount of operands (" + phi.Operands.Count + ") as predecessors (" + predecessors.Count() + "): " + phi.ToString());
                    }

                    if (phi.Operands.Count < 1)
                    {
                        throw new VerifierException("Phi instruction must have at least one entry: " + phi.ToString());
                    }
                }
                else
                {
                    canBePhiNode = false;
                }
            }
        }
    }
    public class VerifierException : Exception
    {
        public VerifierException()
        {
        }

        public VerifierException(string message)
            : base(message)
        {
        }

        public VerifierException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
