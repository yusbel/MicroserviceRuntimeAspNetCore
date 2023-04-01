using Sample.Sdk.Data.Msg;
using Sample.Sdk.Msg.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk
{
    public static class MessageNotifier<T> where T : ExternalMessage
    {
        private static ConcurrentDictionary<string, List<Func<T, bool>>> registers = new ConcurrentDictionary<string, List<Func<T, bool>>>();
        public static bool? Register(Type type, Func<T, bool> func)
        {
            registers.AddOrUpdate(type.FullName, (addKey) => new List<Func<T, bool>>() { func }
            , (updateKey, updateFunc) =>
            {
                updateFunc.Add(func);
                return updateFunc;
            });
            return true;
        }

        public static void Notify(T msg)
        {
            var funcs = registers.Where(f => f.Key == msg.GetType().FullName).Select(item => item.Value).FirstOrDefault();
            funcs?.ForEach(f => f.Invoke(msg));
        }
    }
}
