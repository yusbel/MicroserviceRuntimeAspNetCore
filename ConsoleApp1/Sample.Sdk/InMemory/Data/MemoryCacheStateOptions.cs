using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.InMemory.Data
{
    public class CacheEntryState
    {
        public int AbsoluteExpirationOnSeconds { get; init; }
        public int SlidingExpirationOnSeconds { get; init; }
    }
}
