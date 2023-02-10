using Sample.Sdk;
using Sample.Sdk.Msg;
using System.Collections.Concurrent;

namespace Sample.Messaging.Bus
{
    /// <summary>
    /// Registered as a single instance in the DI
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InMemoryMessageBus<T> : IInMemoryMessageBus<T> where T : class
    {
        //Each subscriber have a ienumerable of messages
        private ConcurrentDictionary<string, IEnumerable<T>> messages = new ConcurrentDictionary<string, IEnumerable<T>>();
        public void Add(string msgKey, T msg)
        {
            Guard.ThrowWhenNull(msgKey, msg);
            var toAdd = new List<T>() { msg };
            if (messages.TryGetValue(msgKey, out var msgs))
            {
                var newMsgs = msgs.ToList();
                newMsgs.AddRange(toAdd);
                messages.TryUpdate(msgKey, newMsgs, msgs);
                return;
            }
            messages.TryAdd(msgKey, toAdd);
        }
        public IEnumerable<T> GetAndRemove(string msgKey)
        {
            return messages.TryRemove(msgKey, out var msgs) ? msgs : new List<T>();
        }
        public bool TryGetAndRemove(string msgKey, out IEnumerable<T> result)
        {
            result = GetAndRemove(msgKey).AsEnumerable();
            return result == Enumerable.Empty<T>() ? false : true;
        }
    }
}