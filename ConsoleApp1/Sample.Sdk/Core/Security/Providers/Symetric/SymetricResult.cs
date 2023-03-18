using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Symetric
{
    public class SymetricResult
    {
        public byte[] PlainData { get; init; }
        public byte[] EncryptedData { get; init; }
        public byte[] Key { get; init; }
        public byte[] Nonce { get; init; }
        public byte[] Tag { get; init; }
        public byte[] Aad { get; init; }
    }
}
