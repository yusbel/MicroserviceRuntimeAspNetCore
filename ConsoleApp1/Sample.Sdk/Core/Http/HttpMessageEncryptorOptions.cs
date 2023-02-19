using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Http
{
    /// <summary>
    /// $Env:AZURE_CLIENT_ID="51df4bce-6532-4345-9be7-5be7af315003"
    /// $Env:AZURE_CLIENT_SECRET="tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2"
    /// $Env:AZURE_TENANT_ID="c8656f45-daf5-42c1-9b29-ac27d3e63bf3"
    /// </summary>
    public class HttpMessageEncryptorOptions
    {
        public string ClientId { get; set; } = "51df4bce-6532-4345-9be7-5be7af315003";
        public string ClientSecret { get; set; } = "tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2";
        public string TenantId { get; set; } = "c8656f45-daf5-42c1-9b29-ac27d3e63bf3";
        public Uri VaultUri { get; set; } = new Uri("https://learningkeyvaultyusbel.vault.azure.net/");
        public string KeyIdentifier { get; set; } = "HttpMessageEncryptionKey";
    }
}
