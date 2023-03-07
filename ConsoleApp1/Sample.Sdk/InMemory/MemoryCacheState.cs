using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Sample.Sdk.InMemory.Data;
using Sample.Sdk.InMemory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.InMemory
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
