using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers
{
    internal interface IPublicKeyProvider
    {
        Task<byte[]> GetPublicKey(Uri endpoint, string publicKeyId);
    }
}
