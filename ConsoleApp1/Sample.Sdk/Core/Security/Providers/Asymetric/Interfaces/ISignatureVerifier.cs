using Sample.Sdk.Core.Security.Providers.Protocol.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces
{
    public interface ISignatureVerifier
    {
        public Task<(bool wasValid, EncryptionDecryptionFail reason)> VerifySignature(byte[] hashValue, byte[] baseSignature);

        public (bool wasValid, EncryptionDecryptionFail reason) VerifySignature(byte[] publicKey, byte[] hashValue, byte[] baseSignature);
        public Task<(bool wasCreated, byte[]? data, EncryptionDecryptionFail reason)> CreateSignature(byte[] baseString);

    }
}
