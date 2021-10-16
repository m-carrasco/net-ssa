using System;

namespace NetSsa.Analyses
{
    public class Variable
    {
        public static readonly String StackVariablePrefix = "s";
        public static readonly String LocalVariablePrefix = "l";
        public static readonly String ArgumentVariablePrefix = "a";
        public static readonly String ExceptionVariablePrefix = "e";

        public string Name;
        public Variable(string name)
        {
            this.Name = name;
        }
    }
}
