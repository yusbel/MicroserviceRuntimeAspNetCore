using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime
{
    public class ServiceDependecyStatus
    {
        public enum Setup 
        {
            None,
            ApplicationOrServicePrincipleNotFound,
            ApplicationIdSecretNotFoundOnKeyVault,

        }
    }
}
