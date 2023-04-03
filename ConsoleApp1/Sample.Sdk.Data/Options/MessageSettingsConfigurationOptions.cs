using Sample.Sdk.Data.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Options
{
    public class MessageSettingsConfigurationOptions
    {
        public static string SectionIdentifier = Environment.GetEnvironmentVariable(ConfigVar.AZURE_MESSAGE_SETTINGS)!;

        public List<AzureMessageSettingsOptions> Sender { get; init; }

        public List<AzureMessageSettingsOptions> Receiver { get; init; }
    }
}
