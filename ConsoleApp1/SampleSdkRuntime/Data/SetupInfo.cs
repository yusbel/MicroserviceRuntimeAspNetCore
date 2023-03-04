using Microsoft.IdentityModel.Tokens;
using SampleSdkRuntime.HostedServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Data
{
    public struct SetupInfo : IRuntimeServiceInfo
    {
        public SetupInfo()
        {
        }

        public string ServiceInstanceIdentifier { get; set; } = string.Empty;
        public bool IsValid { get; set; } = false;
        public IEnumerable<(string, string)> InValidServicesIdWithException { get; set; } = Enumerable.Empty<(string, string)>();
        public IEnumerable<string> ValidServices { get; set; } = Enumerable.Empty<string>();
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public bool IsFaulty { get; set; } = false;
        public ServiceAccountInfo ServiceAccountInfo { get; set; } = new ServiceAccountInfo();
    }
}
