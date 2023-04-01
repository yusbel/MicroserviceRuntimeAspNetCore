using Microsoft.Extensions.Caching.Memory;
using Sample.Sdk.Core.Caching;
using Sample.Sdk.Data.HttpResponse;
using Sample.Sdk.Interface.Security.Keys;

namespace Sample.Sdk.Core.Security.Keys
{
    internal class PublicKeyProvider : IPublicKeyProvider
    {
        private static Lazy<MemoryCacheState<string, byte[]>> publicKeys = new Lazy<MemoryCacheState<string, byte[]>>(() =>
        {
            return new MemoryCacheState<string, byte[]>(new MemoryCache(new MemoryCacheOptions()));
        }, true);

        private readonly HttpClient _httpClient;

        internal PublicKeyProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<byte[]> GetPublicKey(string uri, string keyId, CancellationToken token)
        {
            var uriStr = $"{uri}?key={keyId}";
            if (publicKeys.Value.Cache.TryGetValue<byte[]>(uriStr, out var publicKey))
            {
                return publicKey;
            }
            var response = await _httpClient.GetAsync(new Uri(uriStr), token).ConfigureAwait(false);
            var toReturn = System.Text.Json.JsonSerializer.Deserialize<ServiceResponse<PublicKeyResponse>>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            publicKeys.Value.Cache.Set(uriStr, Convert.FromBase64String(toReturn!.Data.KeyBase64String), TimeSpan.FromHours(5));
            publicKey = publicKeys.Value.Cache.Get<byte[]>(uriStr);
            return publicKey;
        }
    }
}
