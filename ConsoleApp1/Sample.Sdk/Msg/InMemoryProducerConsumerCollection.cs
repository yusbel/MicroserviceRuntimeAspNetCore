using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Sample.Sdk.InMemory;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Sample.Sdk.Msg
{
    /// <summary>
    /// Use BlockingCollection to implement the producer consumer pattern in a thread-safe environment
    /// Use memory cache to keep track of item taked to avoid duplicate for 10 minutes
    /// </summary>
    /// <typeparam name="T">Is a class</typeparam>
    public class InMemoryProducerConsumerCollection<T> : IInMemoryProducerConsumerCollection<T> where T : class, IMessageIdentifier
    {
        private readonly BlockingCollection<T> _state = new BlockingCollection<T>();
        private readonly IMemoryCacheState<string, string> _identifiersCache;
        private readonly ILogger<InMemoryProducerConsumerCollection<T>> _logger;
        private readonly object _lock = new object();

        public int Count => _state.Count;

        public InMemoryProducerConsumerCollection(
            IMemoryCacheState<string, string> identifiersCache,
            ILogger<InMemoryProducerConsumerCollection<T>> logger)
        {
            _identifiersCache = identifiersCache;
            _logger = logger;
        }

        /// <summary>
        /// Thread-safe operation for adding object in a concurrent scneario of adding and removing items
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <returns>True if the item was added otherwise false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public bool TryAdd(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            return _state.TryAdd(item);
        }

        /// <summary>
        /// Thread-safe method to retrieve objects concurrently
        /// </summary>
        /// <param name="item">Object to add</param>
        /// <returns>True when item was retuened otherwise false</returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        public bool TryTake(out T? item)
        {
            try
            {
                if (_state.TryTake(out item))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when taking an item from collection");
            }
            item = default;
            return false;
        }

        /// <summary>
        /// Call this method when processing a fail message and it will be retried again
        /// </summary>
        /// <param name="item">To be removed</param>
        /// <returns></returns>
        public bool TryAddAndRemoveCache(T? item)
        {
            if (item == null || _state == null)
                return false;
            try
            {
                lock (_lock)
                {
                    _identifiersCache.Cache.Remove(item.Id);
                }
                _state.Add(item);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when adding and removing item to memory collection");
                return false;
            }
        }
        /// <summary>
        /// Loop taking item until an item is not found in cache or cancellation request is true.
        /// </summary>
        /// <param name="item">Item from collection</param>
        /// <param name="token">Cancellation token used to stop the loop</param>
        /// <returns></returns>
        public bool TryTakeDeDuplicate(out T? item, CancellationToken token)
        {
            try
            {
                while (_state.TryTake(out item) && !token.IsCancellationRequested)
                {
                    if (!_identifiersCache.Cache.TryGetValue(item.Id, out string foundItem))
                    {
                        lock (_lock)
                        {
                            _identifiersCache.Cache.Set(
                                item.Id,
                                string.Empty,
                                absoluteExpiration: DateTimeOffset.UtcNow.AddMinutes(10));
                        }
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(e, "An error ocurred when pulling objects from in memory collection");
            }
            item = default;
            return false;
        }
        /// <summary>
        /// Use for loop
        /// </summary>
        /// <param name="pageSize">Return a enumerable limited to page size. When pageSize is equal or less than cero it return all items found</param>
        /// <param name="items">IEnumerable of items</param>
        /// <param name="token">Cancellation token</param>
        /// <returns></returns>
        public bool TryTakeAllWithoutDuplicate(out IList<T> items, CancellationToken token, int pageSize = 0)
        {
            _state.GetConsumingEnumerable();
            int counter = 0;
            items = new List<T>();
            while ((counter < pageSize || pageSize <= 0) //page condition
                && TryTakeDeDuplicate(out var item, token))
            {
                items.Add(item!);
                if (!(pageSize <= 0))
                    counter++;
            }
            return (items.Count > 0);
        }
    }
}
