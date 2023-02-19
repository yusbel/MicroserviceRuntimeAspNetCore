using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.Providers.Symetric
{
    public class SymetricResult
    {
        public byte[] PlainData { get; set; }
        public byte[] EncryptedData { get; set; }
        public byte[] Key { get; set; }
        public byte[] Iv { get; set; }
    }
}
