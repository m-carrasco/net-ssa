using System;

namespace NetSsa.Queries
{
    public class SsaQueryException : Exception
    {
        public String InternalMessage;
        public String Binary;
        public String[] Arguments;
        public String StandardError;

        public SsaQueryException()
        {
        }

        public SsaQueryException(String binary, String[] arguments, String standardError)
           : base("Query exited with error code.")
        {
            InternalMessage = String.Format("Execution of binary '{0}' with arguments '{1}' failed. Standard error is '{2}'.", binary, String.Join(" ", arguments), standardError);
            Binary = binary;
            Arguments = arguments;
            StandardError = standardError;
        }

        public SsaQueryException(string message)
            : base(message)
        {
        }

        public SsaQueryException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}