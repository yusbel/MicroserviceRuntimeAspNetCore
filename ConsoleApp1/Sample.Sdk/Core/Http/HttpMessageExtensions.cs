using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Http
{
    public static class HttpMessageExtensions
    {
        public static HttpResponseMessage EncryptMessage(this HttpResponseMessage responseMessage) 
        {
            return responseMessage;
        }

        public static HttpRequestMessage DecryptMessage(this HttpRequestMessage requestMessage) 
        {
            return requestMessage;
        }
    }
}
