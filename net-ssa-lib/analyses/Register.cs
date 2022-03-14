using System;
using System.Collections.Generic;
using NetSsa.Instructions;

namespace NetSsa.Analyses
{
    public class Register : ValueContainer
    {
        public static readonly Register UndefinedRegister = new Register("undefined");

        public static readonly string StackSlotPrefix = "s";

        public static readonly string ExceptionPrefix = "e";

        public bool IsException = false;

        public Register(uint idx) : this(StackSlotPrefix + idx)
        {

        }
        public Register(string name) : base(name)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

    }
}
