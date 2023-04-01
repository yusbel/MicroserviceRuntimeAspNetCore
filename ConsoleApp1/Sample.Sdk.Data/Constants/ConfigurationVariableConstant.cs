using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Data.Constants
{
    public class ConfigVarConst
    {
        public const string IS_RUNTIME = "IsRuntime";
        public const string AZURE_TENANT_ID = "AZURE_TENANT_ID";
        public const string AZURE_CLIENT_ID = "AZURE_CLIENT_ID";
        public const string AZURE_CLIENT_SECRET = "AZURE_CLIENT_SECRET";

        public const string RUNTIME_AZURE_TENANT_ID = "RUNTIME:AZURE_TENANT_ID";
        public const string RUNTIME_AZURE_CLIENT_ID = "RUNTIME:AZURE_CLIENT_ID";
        public const string RUNTIME_AZURE_CLIENT_SECRET = "RUNTIME:AZURE_CLIENT_SECRET";

        public const string SERVICE_INSTANCE_NAME_ID = "SERVICE_INSTANCE_NAME_ID";
        public const string RUNTIME_SETUP_INFO = "SetupInfo";

        public const string ENVIRONMENT_VAR = "NETCORE_ENVIRONMENT";

        public const string APP_CONFIG_CONN_STR = "APP_CONFIG_CONN_STR";

        public const string SERVICE_DATA_BLOB_CONTAINER_NAME = "SERVICE_DATA_BLOB_CONTAINER_NAME";

        public const string SERVICE_BLOB_CONN_STR_APP_CONFIG_KEY = "SERVICE_BLOB_CONN_STR_APP_CONFIG_KEY";
        public const string BLOB_CERTIFICATE_PATH_APP_CONFIG_KEY = "BLOB_CERTIFICATE_PATH_APP_CONFIG_KEY";
        public const string SERVICE_RUNTIME_CERTIFICATE_NAME_APP_CONFIG_KEY = "SERVICE_RUNTIME_CERTIFICATE_NAME_APP_CONFIG_KEY";
        public const string SERVICE_RUNTIME_BLOB_CONN_STR_KEY = "SERVICE_RUNTIME_BLOB_CONN_STR_KEY";
        public const string RUNTIME_BLOB_PUBLICKEY_CONTAINER_NAME = "RUNTIME_BLOB_PUBLICKEY_CONTAINER_NAME";
    }
}
