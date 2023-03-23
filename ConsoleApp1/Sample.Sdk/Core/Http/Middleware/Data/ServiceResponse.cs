using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Http.Middleware.Data
{
    public class ServiceResponse<T>
    {
        public string Error { get; init; } = string.Empty;
        public T Data { get; init; } 
    }
}
