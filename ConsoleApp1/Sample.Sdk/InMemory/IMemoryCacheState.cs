using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.InMemory
{
    public interface IMemoryCacheState<TKey, T>
    {
        IMemoryCache Cache { get; }
        //void AddEntry(
        //    TKey key
        //    , T entry
        //    , Action<TKey, T> onCacheEntryEnvicted);
        //void Remove(TKey key);
        //bool TryGetValue(TKey key, out T value);
    }
}
