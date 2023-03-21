using Sample.Sdk.Core.Security.Providers.Symetric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class StringExtensions
    {
        public static SymetricResult ToSymetricResult(this string base64String) 
        {
            var symetricResultDto = System.Text.Json.JsonSerializer.Deserialize<SymetricResultDto>(base64String);
            var symetricResult = new SymetricResult()
                            {
                                EncryptedData = Convert.FromBase64String(symetricResultDto.EncryptedData)
                            };
            for(var i = 0; i < symetricResultDto!.Key.Count; i++) 
            {
                symetricResult.Key.Add(Convert.FromBase64String(symetricResultDto.Key[i]));
                symetricResult.Nonce.Add(Convert.FromBase64String(symetricResultDto.Nonce[i]));
                symetricResult.Tag.Add(Convert.FromBase64String(symetricResultDto.Tag[i]));
            }
            return symetricResult;
        }
    }
}
