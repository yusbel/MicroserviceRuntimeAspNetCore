using Microsoft.Extensions.Caching.Memory;

namespace Sample.Sdk.Interface.Caching
{
    public interface IMemoryCacheState<TKey, T>
    {
        IMemoryCache Cache { get; }
    }
}
