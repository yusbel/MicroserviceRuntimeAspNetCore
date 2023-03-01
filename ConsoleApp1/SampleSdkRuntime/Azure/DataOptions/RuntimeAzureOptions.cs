using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure.DataOptions
{
    public class RuntimeAzureOptions
    {
        public RuntimeKeyVaultOptions RuntimeKeyVaultOptions { get; set; }

        public static RuntimeAzureOptions CreateDefault() 
        { 
            return new RuntimeAzureOptions() 
            { 
                RuntimeKeyVaultOptions = new RuntimeKeyVaultOptions() 
            }; 
        }
    }
}
