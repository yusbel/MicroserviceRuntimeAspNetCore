using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Exceptions
{
    public class RuntimeStartException : Exception
    {
        public RuntimeStartException(string? message) : base(message)
        {
        }

        public RuntimeStartException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
