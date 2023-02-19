using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces
{
    public interface ISignatureVerifier
    {
        public Task<bool> VerifySignature(byte[] hashValue, byte[] baseSignature);

        public bool VerifySignature(byte[] publicKey, byte[] hashValue, byte[] baseSignature);
        public Task<byte[]> CreateSignature(byte[] baseString);

    }
}
