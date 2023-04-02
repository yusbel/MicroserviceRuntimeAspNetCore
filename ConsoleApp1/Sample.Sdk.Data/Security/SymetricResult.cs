using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Security
{
    public class SymetricResult
    {
        public byte[] PlainData { get; set; }
        public byte[] EncryptedData { get; set; }
        public List<byte[]> Key { get; init; } = new List<byte[]>();
        public List<byte[]> Nonce { get; init; } = new List<byte[]>();
        public List<byte[]> Tag { get; init; } = new List<byte[]>();
        public byte[] Aad { get; init; }
    }

}
