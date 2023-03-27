using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Msg.Interfaces
{
    /// <summary>
    /// Thread safe collection without cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InMemoryCollection<T> : IInMemoryCollection<T> where T : class
    {
        private readonly BlockingCollection<T> _state;
        public InMemoryCollection()
        {
            _state = new BlockingCollection<T>();
        }

        public void Add(T item)
        {
            if (item == null) { return; }
            _state.Add(item);
        }
        public bool TryAdd(T item)
        {
            if (item == null) { return false; }
            return _state.TryAdd(item);
        }
        public bool TryTake(out T? item)
        {
            return _state.TryTake(out item);
        }
        public T Take()
        {
            return _state.Take();
        }

        public bool TryTakeAll(out List<T> items) 
        {
            items = new List<T>();
            while (TryTake(out var item))
            {
                if(item != null)
                    items.Add(item);
            }
            return items.Any();
        }
    }
}
