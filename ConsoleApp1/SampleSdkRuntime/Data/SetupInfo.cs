using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Data
{
    public class SetupInfo : IRuntimeServiceInfo
    {   
        public bool IsValid { get; set; }
        public IEnumerable<(string, string)> InValidServicesIdWithException { get; set; }
        public IEnumerable<string> ValidServices { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsFaulty { get; set; }
    }
}
