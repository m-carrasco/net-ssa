using System;
using System.Collections.Generic;
using NetSsa.Instructions;
using Mono.Cecil;

namespace NetSsa.Analyses
{
    public enum MemoryVariableKind
    {
        LocalVariable,
        ArgumentVariable,
    }

    public class MemoryVariable : ValueContainer
    {
        public static readonly String LocalVariablePrefix = "l";
        public static readonly String ArgumentVariablePrefix = "a";
        public MemoryVariableKind Kind;
        public TypeReference Type;

        public MemoryVariable(string name, TypeReference t, MemoryVariableKind k) : base(name)
        {
            this.Kind = k;
            this.Type = t;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
