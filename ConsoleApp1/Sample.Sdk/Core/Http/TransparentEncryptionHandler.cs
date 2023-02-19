using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sample.Sdk.Core.Http.Interfaces;

namespace Sample.Sdk.Core.Http
{
    /// <summary>
    /// It will be injected using a factory on an assembly that is visible to. 
    /// </summary>
    internal class TransparentEncryptionHandler : DelegatingHandler
    {
        private readonly IHttpMessageEncryptor _encryptor;

        public TransparentEncryptionHandler(IHttpMessageEncryptor encryptor) 
        {
            _encryptor = encryptor;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            
            return base.SendAsync(request, cancellationToken);
        }
    }
}
