namespace Sample.Sdk.Http.Interfaces
{
    internal interface IHttpMessageEncryptor
    {
        public Task<HttpRequestMessage> Encrypt(HttpRequestMessage request, CancellationToken token);
        public Task<HttpResponseMessage> Encrypt(HttpResponseMessage request, CancellationToken token);
        public Task<HttpRequestMessage> Decrypt(HttpRequestMessage request, CancellationToken token);
        public Task<HttpResponseMessage> Decrypt(HttpResponseMessage request, CancellationToken token);
    }
}