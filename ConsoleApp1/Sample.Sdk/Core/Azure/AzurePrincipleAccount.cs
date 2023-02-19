using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Azure
{
    /// <summary>
    /// Encapsulate options to create azure client.
    /// </summary>
    public class AzurePrincipleAccount
    {
        public const string SectionIdentifier = "ServiceSdk:AzurePrincipleAccount";
        public string AZURE_TENANT_ID { get; set; }
        public string AZURE_CLIENT_ID { get; set; }
        public string AZURE_CLIENT_SECRET { get; set; }
    }
}
