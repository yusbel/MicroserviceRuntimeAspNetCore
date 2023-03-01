using Azure.Identity;
using Azure.ResourceManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.Factory
{
    public class ArmClientFactory : IArmClientFactory
    {
        private readonly ClientSecretCredential _clientSecretCredential;

        public ArmClientFactory(ClientSecretCredential clientSecretCredential)
        {
            _clientSecretCredential = clientSecretCredential;
        }

        public ArmClient Create()
        {
            return new ArmClient(_clientSecretCredential);
        }
    }
}
