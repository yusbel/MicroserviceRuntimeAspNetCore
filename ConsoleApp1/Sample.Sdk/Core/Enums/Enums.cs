using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Enums
{
    public class Enums
    {
        public enum StringType { WithPlainDataOnly, WithEncryptedDataOnly, WithoutPlainAndEncryptedData }

        public enum AzureKeyVaultOptionsType { Runtime, ServiceInstance }
    }
}
