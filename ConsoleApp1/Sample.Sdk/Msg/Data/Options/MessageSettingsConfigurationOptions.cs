using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Data.Options
{
    public class MessageSettingsConfigurationOptions
    {
        public const string SECTION_ID = "Service:AzureMessageSettings:Configuration";

        public List<AzureMessageSettingsOptions> Sender { get; init; }

        public List<AzureMessageSettingsOptions> Receiver { get; init; }
    }
}
