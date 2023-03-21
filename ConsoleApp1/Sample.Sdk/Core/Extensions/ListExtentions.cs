using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class ListExtentions
    {
        public static List<Task> AddTaskWithConfigureAwaitFalse(this List<Task> taskList, params Task[] tasks) 
        {
            taskList.AddRange(tasks);
            taskList.ForEach(task => { task.ConfigureAwait(false); });
            return taskList;
        }

        public static (string key, string value) ConvertToString(this List<KeyValuePair<byte[], byte[]>> list) 
        {
            var strKey = list.Select(item => item.Key).ToList().ConvertAll(item => Convert.ToBase64String(item));
            var strValue = list.Select(item=> item.Value).ToList().ConvertAll(item => Convert.ToBase64String(item));
            return (string.Join("-", strKey), string.Join("-", strValue));
        }

        public static void AddKeyValueFromString(this List<KeyValuePair<byte[], byte[]>> list, string encodedKeys, string encodedValues) 
        {
            var strKeys = encodedKeys.Split('-').ToList().ConvertAll(item=> Convert.FromBase64String(item));
            var strValues = encodedValues.Split('-').ToList().ConvertAll(item=> Convert.FromBase64String(item));
            for(var i = 0; i < strKeys.Count; i++) 
            {
                list.Add(KeyValuePair.Create(strKeys[i], strValues[i]));
            }
        }
    }
}
