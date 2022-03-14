using System;
using System.Collections.Generic;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public enum MemoryVariableKind
    {
        Local,
        Argument,
        Exception
    }

    public class MemoryVariable : ValueContainer
    {
        public static readonly String LocalVariablePrefix = "l";
        public static readonly String ArgumentVariablePrefix = "a";
        public MemoryVariableKind Kind;

        public MemoryVariable(string name, MemoryVariableKind k) : base(name)
        {
            this.Kind = k;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
