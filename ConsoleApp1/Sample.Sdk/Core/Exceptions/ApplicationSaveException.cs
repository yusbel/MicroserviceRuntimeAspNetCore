using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Exceptions
{
    public class ApplicationSaveException : ApplicationException
    {
        private readonly string? _message;
        private readonly Exception? _innerException;

        public ApplicationSaveException(string? message) : base(message)
        {
            _message = message;
        }

        public ApplicationSaveException(string? message, Exception? innerException) : 
            base(message, innerException) 
        {
            _message = message;
            _innerException = innerException;
        }
    }
}
