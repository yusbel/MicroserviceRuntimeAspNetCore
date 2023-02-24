namespace Sample.Sdk.Core.Security.Providers.Protocol.Http
{
    public interface IHttpClientResponseConverter
    {
        Task<(bool isValid, T? data, TInvalid? invalidResponse)> InvokePost<T, TInvalid>(
            Uri uri, 
            HttpContent content,
            CancellationToken token)
            where T : class
            where TInvalid : class;
    }
}