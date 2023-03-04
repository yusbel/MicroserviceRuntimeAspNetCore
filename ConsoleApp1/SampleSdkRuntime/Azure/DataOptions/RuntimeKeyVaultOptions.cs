using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.DataOptions
{
    public class RuntimeKeyVaultOptions
    {
        public string KeyVaultResourceId { get; set; } = "/subscriptions/e2ad7149-754e-4628-a8dc-54a49b116708/resourceGroups/LearningServiceBus-RG/providers/Microsoft.KeyVault/vaults/learningKeyVaultYusbel";
        public string KeyVaultStringUri { get; set; } = "https://learningkeyvaultyusbel.vault.azure.net/";
    }
}
