using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Sample.Sdk.InMemory.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.InMemory
{
    //Client use IMemoryExtension methods
    public class MemoryCacheState<TKey, T> : IMemoryCacheState<TKey, T> where TKey : class where T : CacheEntryState
    {
        private readonly IMemoryCache _memoryCache;
        public MemoryCacheState(IMemoryCache memoryCache) 
        {
            _memoryCache = memoryCache;
        }

        public IMemoryCache Cache => _memoryCache;

        //public void AddEntry(
        //    TKey key
        //    , T entry
        //    , Action<TKey, T>? onCacheEntryEnvicted = null)
        //{
        //    var cacheEntry = _memoryCache.CreateEntry(key);
        //    cacheEntry.Value = entry;
        //    _memoryCache.GetOrCreateAsync()
        //    if(entry.AbsoluteExpirationOnSeconds > 0) 
        //    {
        //        cacheEntry.SetAbsoluteExpiration(TimeSpan.FromSeconds(entry.AbsoluteExpirationOnSeconds));
        //    }
        //    if(entry.SlidingExpirationOnSeconds > 0) 
        //    {
        //        cacheEntry.SetSlidingExpiration(TimeSpan.FromSeconds(entry.SlidingExpirationOnSeconds));
        //    }
        //    if(onCacheEntryEnvicted != null) 
        //    {
        //        cacheEntry.PostEvictionCallbacks.Add(
        //            new PostEvictionCallbackRegistration()
        //            {
        //                EvictionCallback = (key, obj, reason, state) =>
        //                {
        //                    if (((key as TKey) != null) && ((obj as T) != null))
        //                    {
        //                        onCacheEntryEnvicted(key as TKey, obj as T);
        //                    }
        //                }
        //            });
        //    }

        //    _memoryCache.
        //}

        //public void Remove(TKey key)
        //{
        //    _memoryCache.Remove(key);
        //}

        //public bool TryGetValue(TKey key, out T value)
        //{
        //    if (_memoryCache.TryGetValue(key, out object result)) 
        //    {
        //        if(result != null && result as T != null) 
        //        {
        //            value = (T)result;
        //            return true;
        //        }
        //    }
        //    value = null;
        //    return false;
        //}
    }
}
