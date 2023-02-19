using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Symetric.Interface
{
    public interface ISymetricCryptoProvider
    {
        bool TryDecrypt(byte[] data, byte[] key, byte[] iv, out SymetricResult result);
        bool TryEncrypt(byte[] data, out SymetricResult result);
    }
}
