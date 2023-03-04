using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using SampleSdkRuntime.Data;
using System.Text;
using System.Text.Json;

namespace SampleSdkRuntime.HostedServices
{
    public class RuntimeHostedServiceBase : IDisposable
    {
        private readonly IConfiguration _configuration;
        private SemaphoreSlim? _semaphore = null;
        private SetupInfo? _setupInfo;

        public RuntimeHostedServiceBase(IConfiguration configuration)
        {
            _configuration = configuration;
            _semaphore = new SemaphoreSlim(1);
        }
        protected void CreateSetupInfo(SetupInfo setupInfo, bool wasCreated, Application? app, string? clientSecret)
        {
            setupInfo.IsValid = wasCreated;
            setupInfo.CreatedOn = DateTime.UtcNow;
            setupInfo.IsFaulty = !wasCreated;
            setupInfo.InValidServicesIdWithException = new List<(string, string)>() { (String.Empty, String.Empty) };
            setupInfo.ServiceAccountInfo = new ServiceAccountInfo()
            {
                ApplicationClientId = app == null ? string.Empty : app!.AppId,
                ClientSecret = string.IsNullOrEmpty(clientSecret) ? string.Empty : clientSecret,
                TenantId = _configuration.GetValue<string>(ServiceRuntime.RUNTIME_AZURE_TENANT_ID)
            };
            SetSetupInfo(setupInfo);
            SetEnvironmentVariableForHostService();
        }
        protected SetupInfo? GetSetupInfo()
        {
            _semaphore!.Wait();
            try
            {
                return _setupInfo;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        protected void SetSetupInfo(SetupInfo setupInfo)
        {
            _semaphore!.Wait();
            try
            {
                _setupInfo = setupInfo;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected void SetEnvironmentVariableForHostService()
        {
            var setupInfo = GetSetupInfo();
            //Environment.SetEnvironmentVariable(ServiceRuntime.AZURE_TENANT_ID, setupInfo!.Value.ServiceAccountInfo.TenantId);
            //Environment.SetEnvironmentVariable(ServiceRuntime.AZURE_CLIENT_ID, setupInfo!.Value.ServiceAccountInfo.ApplicationClientId);
            //Environment.SetEnvironmentVariable(ServiceRuntime.AZURE_CLIENT_SECRET, setupInfo!.Value.ServiceAccountInfo.ClientSecret);
            Environment.SetEnvironmentVariable(ServiceRuntime.SERVICE_INSTANCE_ID, setupInfo!.Value.ServiceInstanceIdentifier);
            Environment.SetEnvironmentVariable(ServiceRuntime.RUNTIME_SETUP_INFO, Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(setupInfo))));
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}