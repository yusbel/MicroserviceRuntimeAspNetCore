using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers
{
    public interface IPublicKeyProvider
    {
        Task<byte[]> GetPublicKey(string uri, string keyId, CancellationToken token);
    }
}
