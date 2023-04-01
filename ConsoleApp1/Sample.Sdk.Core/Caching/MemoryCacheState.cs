using Microsoft.Extensions.Caching.Memory;
using Sample.Sdk.Interface.Caching;

namespace Sample.Sdk.Core.Caching
{
    //Client use IMemoryExtension methods
    public class MemoryCacheState<TKey, T> : IMemoryCacheState<TKey, T> where TKey : class
    {
        private readonly IMemoryCache _memoryCache;
        public MemoryCacheState(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public static IMemoryCacheState<TKey, T> Instance()
        {
            return new MemoryCacheState<TKey, T>(new MemoryCache(new MemoryCacheOptions()));
        }

        public IMemoryCache Cache => _memoryCache;
    }
}
