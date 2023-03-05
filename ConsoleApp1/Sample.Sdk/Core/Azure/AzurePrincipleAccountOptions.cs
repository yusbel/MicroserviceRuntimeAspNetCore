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
    public class AzurePrincipleAccountOptions
    {
        public const string SERVICE_INSTANCE_SECTION = "ServiceSdk:ServicePrinciple";
        public const string RUNTIME_SECTION = "ServiceRuntime:RuntimePrincipal";
        public string AZURE_TENANT_ID { get; set; }
        public string AZURE_CLIENT_ID { get; set; }
        public string AZURE_CLIENT_SECRET { get; set; }
    }
}
