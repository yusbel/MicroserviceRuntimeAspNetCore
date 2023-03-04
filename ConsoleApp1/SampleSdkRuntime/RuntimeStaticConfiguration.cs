using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime
{
    public class RuntimeStaticConfiguration
    {
        private readonly IConfiguration _config;

        public RuntimeStaticConfiguration(IConfiguration config)
        {
            _config = config;
        }

        public string KeyVaultResourceIdentifier 
        {
            get => _config.GetValue<string>("") ?? "/subscriptions/e2ad7149-754e-4628-a8dc-54a49b116708/resourceGroups/LearningServiceBus-RG/providers/Microsoft.KeyVault/vaults/learningKeyVaultYusbel";
        }
    }
}
