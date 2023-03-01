using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleSdkRuntime.Azure
{
    public class ActiveDirectoryService
    {
        public ActiveDirectoryService() { }

        public async Task RunAsync() 
        {
            await SetupPrincipleAccounts();
        }

        /// <summary>
        /// Use a principle account to query AzureKeyVault to retrieve principle accounts for different services using 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private Task SetupPrincipleAccounts()
        {
            throw new NotImplementedException();
        }
    }
}
