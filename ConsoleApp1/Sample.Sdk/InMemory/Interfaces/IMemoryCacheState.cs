using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.InMemory.Interfaces
{
    public interface IMemoryCacheState<TKey, T>
    {
        IMemoryCache Cache { get; }
    }
}
