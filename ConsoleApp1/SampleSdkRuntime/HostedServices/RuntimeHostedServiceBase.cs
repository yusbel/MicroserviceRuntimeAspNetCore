using Microsoft.Extensions.Configuration;
using Sample.Sdk.Data.Constants;
using SampleSdkRuntime.Data;
using System.Text;
using System.Text.Json;

namespace SampleSdkRuntime.HostedServices
{
    public class RuntimeHostedServiceBase : IDisposable
    {
        private readonly IConfiguration _configuration;
        private SemaphoreSlim? _semaphore = null;
        private ServiceRegistration? _serviceReg;

        public RuntimeHostedServiceBase(IConfiguration configuration)
        {
            _configuration = configuration;
            _semaphore = new SemaphoreSlim(1);
        }
        protected void CreateSetupInfo(ServiceRegistration serviceReg)
        {
            serviceReg.WasSuccessful = serviceReg != null;
            SetServiceReg(serviceReg);
            SetEnvironmentVariableForHostService();
        }
        protected ServiceRegistration? GetServiceReg()
        {
            _semaphore!.Wait();
            try
            {
                return _serviceReg;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        protected void SetServiceReg(ServiceRegistration serviceReg)
        {
            _semaphore!.Wait();
            try
            {
                _serviceReg = serviceReg;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected void SetEnvironmentVariableForHostService()
        {
            var serviceReg = GetServiceReg();
            //Environment.SetEnvironmentVariable(ServiceRuntime.AZURE_TENANT_ID, Environment.GetEnvironmentVariable(ServiceRuntime.AZURE_TENANT_ID)); ;
            //Environment.SetEnvironmentVariable(ServiceRuntime.AZURE_CLIENT_ID, GetServiceReg().Credentials.First().ClientId);
            //Environment.SetEnvironmentVariable(ServiceRuntime.AZURE_CLIENT_SECRET, GetServiceReg().Credentials.First().ServiceSecretText);
            Environment.SetEnvironmentVariable(ConfigVarConst.SERVICE_INSTANCE_NAME_ID, serviceReg.ServiceInstanceId);
            Environment.SetEnvironmentVariable(ConfigVarConst.RUNTIME_SETUP_INFO, Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(serviceReg))));
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}