using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Exceptions
{
    public class SenderQueueNotRegisteredException : Exception
    {
        public SenderQueueNotRegisteredException() : this(string.Empty) { }

        public SenderQueueNotRegisteredException(string? message) : this(message, null)
        {
        }

        public SenderQueueNotRegisteredException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
