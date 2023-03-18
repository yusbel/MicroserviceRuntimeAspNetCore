using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Constants
{
    public class ConfigurationVariableConstant
    {
        public const string IS_RUNTIME = "IsRuntime";
        public const string AZURE_TENANT_ID = "AZURE_TENANT_ID";
        public const string AZURE_CLIENT_ID = "AZURE_CLIENT_ID";
        public const string AZURE_CLIENT_SECRET = "AZURE_CLIENT_SECRET";

        public const string RUNTIME_AZURE_TENANT_ID = "RUNTIME:AZURE_TENANT_ID";
        public const string RUNTIME_AZURE_CLIENT_ID = "RUNTIME:AZURE_CLIENT_ID";
        public const string RUNTIME_AZURE_CLIENT_SECRET = "RUNTIME:AZURE_CLIENT_SECRET";

        public const string SERVICE_INSTANCE_ID = "SERVICE_INSTANCE_ID";
        public const string RUNTIME_SETUP_INFO = "SetupInfo";
    }
}
