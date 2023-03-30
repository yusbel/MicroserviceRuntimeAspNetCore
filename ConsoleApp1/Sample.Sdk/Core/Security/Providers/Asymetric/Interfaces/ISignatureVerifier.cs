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
        public Task<(bool wasValid, EncryptionDecryptionFail reason)> VerifySignature(byte[] hashValue, byte[] baseSignature, 
            Enums.Enums.HostTypeOptions options,
            string certificateName,
            CancellationToken token);

        public (bool wasValid, EncryptionDecryptionFail reason) VerifySignature(byte[] publicKey, byte[] hashValue, byte[] baseSignature, CancellationToken token);
        public Task<(bool wasCreated, byte[]? data, EncryptionDecryptionFail reason)> CreateSignature(byte[] baseString, 
            Enums.Enums.HostTypeOptions options,
            CancellationToken token);

    }
}
