using Sample.Sdk.Data.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Interface.Security.Symetric
{
    public interface ISymetricCryptoProvider
    {
        bool TryDecrypt(byte[] cypherText, byte[] key, byte[] tag, byte[] iv, byte[] aad, out SymetricResult? result);
        bool TryEncrypt(byte[] plainText, byte[] additionalData, out SymetricResult? result);

        bool TryEncrypt(byte[] key, byte[] plainText, byte[] aad, out SymetricResult? result);

    }
}
