using Sample.Sdk.Data.Constants;

namespace SampleSdkRuntime.Host
{
    public class ConfigureEnvironmentVariables
    {
        public static void AssignEnvironmentVariables(string[] args)
        {
            Environment.SetEnvironmentVariable(ConfigVar.AZURE_TENANT_ID, "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");
            Environment.SetEnvironmentVariable(ConfigVar.ENVIRONMENT_VAR, "Development");
            Environment.SetEnvironmentVariable(ConfigVar.AZURE_CLIENT_ID, "0f691c02-1c41-4783-b54c-22d921db4e16");
            Environment.SetEnvironmentVariable(ConfigVar.AZURE_CLIENT_SECRET, "HuU8Q~UGJXdLK3b4hyM1XFnQaP6BVeOLVIJOia_x");
            Environment.SetEnvironmentVariable(ConfigVar.APP_CONFIG_CONN_STR, "Endpoint=https://learningappconfig.azconfig.io;Id=pIlK-ll-s0:SMHTAi4UoZxaK1C0ADZg;Secret=5cx53U0WM7bLwCcoJ2nM0oit+B1MK7UUsbWA9p6z3KY=");
            Environment.SetEnvironmentVariable(ConfigVar.SERVICE_DATA_BLOB_CONTAINER_NAME, "servicedata");
            Environment.SetEnvironmentVariable(ConfigVar.SERVICE_BLOB_CONN_STR_APP_CONFIG_KEY, "MessageSignatureBlobConnSt");
            Environment.SetEnvironmentVariable(ConfigVar.SERVICE_INSTANCE_NAME_ID, $"{args[0]}-{args[1]}");
            Environment.SetEnvironmentVariable(ConfigVar.BLOB_CERTIFICATE_PATH_APP_CONFIG_KEY, "ServiceRuntime:BlobCertificatePath");
            Environment.SetEnvironmentVariable(ConfigVar.SERVICE_RUNTIME_CERTIFICATE_NAME_APP_CONFIG_KEY, "ServiceRuntime:SignatureCertificateName");
            Environment.SetEnvironmentVariable(ConfigVar.SERVICE_RUNTIME_BLOB_CONN_STR_KEY, "ServiceRuntime:BlobPublicKeyConnStr");
            Environment.SetEnvironmentVariable(ConfigVar.RUNTIME_BLOB_PUBLICKEY_CONTAINER_NAME, "ServiceRuntime:BlobPublicKeys:ContainerName");
            Environment.SetEnvironmentVariable(ConfigVar.DB_CONN_STR, "DbConnectionString");
            Environment.SetEnvironmentVariable(ConfigVar.CUSTOM_PROTOCOL, "ServiceSdk:Security:CustomProtocol");
            Environment.SetEnvironmentVariable(ConfigVar.AZURE_MESSAGE_SETTINGS, "Service:AzureMessageSettings:Configuration");
        }
    }
}
