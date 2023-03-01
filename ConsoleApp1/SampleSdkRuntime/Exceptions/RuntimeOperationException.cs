using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Exceptions
{
    public class RuntimeOperationException : Exception
    {
        public RuntimeOperationException() : this(string.Empty) { }
        public RuntimeOperationException(string message) : this(message, default) { }
        public RuntimeOperationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
