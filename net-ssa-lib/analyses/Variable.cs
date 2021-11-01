using System;

namespace NetSsa.Analyses
{
    public class Variable
    {
        public static readonly String StackVariablePrefix = "s";
        public static readonly String LocalVariablePrefix = "l";
        public static readonly String ArgumentVariablePrefix = "a";
        public static readonly String ExceptionVariablePrefix = "e";

        public static readonly Variable UndefinedVariable = new Variable("undefined");

        public string Name;
        public Variable(string name)
        {
            this.Name = name;
        }

        public bool IsStackVariable()
        {
            return Name.StartsWith(StackVariablePrefix);
        }

        public bool IsArgumentVariable()
        {
            return Name.StartsWith(ArgumentVariablePrefix);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
