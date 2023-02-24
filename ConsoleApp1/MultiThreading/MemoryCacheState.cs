using Microsoft.Extensions.Caching.Memory;

namespace MultiThreading
{
    //Client use IMemoryExtension methods
    public class MemoryCacheState<TKey, T> : IMemoryCacheState<TKey, T> where TKey : class
    {
        private readonly IMemoryCache _memoryCache;
        public MemoryCacheState(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public IMemoryCache Cache => _memoryCache;
    }
}
