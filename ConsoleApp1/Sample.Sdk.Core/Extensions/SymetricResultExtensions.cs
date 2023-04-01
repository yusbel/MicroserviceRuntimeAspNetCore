using Sample.Sdk.Data;

namespace Sample.Sdk.Core.Extensions
{
    public static class SymetricResultExtensions
    {
        public static string ConvertToBase64String(this SymetricResult symetricResult)
        {
            var keys = new List<string>();
            var nonces = new List<string>();
            var tags = new List<string>();
            for (var i = 0; i < symetricResult.Key.Count; i++)
            {
                keys.Add(Convert.ToBase64String(symetricResult.Key[i]));
                nonces.Add(Convert.ToBase64String(symetricResult.Nonce[i]));
                tags.Add(Convert.ToBase64String(symetricResult.Tag[i]));
            }
            var symetricResultDto = new SymetricResultDto()
            {
                Key = keys,
                Tag = tags,
                Nonce = nonces,
                EncryptedData = Convert.ToBase64String(symetricResult.EncryptedData)
            };
            return System.Text.Json.JsonSerializer.Serialize(symetricResultDto);
        }

    }
}
