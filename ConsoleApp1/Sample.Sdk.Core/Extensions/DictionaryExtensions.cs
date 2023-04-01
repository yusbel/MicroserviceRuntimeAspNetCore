using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Extensions
{
    public static class DictionaryExtensions
    {
        public static IDictionary<string, object> ConvertObjectToBase64(this IDictionary<string, object> dict)
        {
            var keys = dict.Keys.ToImmutableList();
            foreach (var key in keys)
            {
                var value = dict[key];
                dict.Remove(key);
                if (value is string strValue && strValue != "[]")
                {
                    dict.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(key)), value);
                }
                else
                {
                    dict.Add(Convert.ToBase64String(Encoding.UTF8.GetBytes(key)), value);
                }
            }
            return dict;
        }

        public static (string key, string value) ConvertDictionaryKeysAndValuesIntoBase64String(this Dictionary<byte[], byte[]> dict)
        {
            var listStr = dict.Keys.ToList().ConvertAll(item => Convert.ToBase64String(item));
            var keysStr = string.Join("-", listStr);
            var listValueStr = dict.Values.ToList().ConvertAll(item => Convert.ToBase64String(item));
            var valuesStr = string.Join("-", listValueStr);
            return (keysStr, valuesStr);
        }

        public static void ConvertEncryptedStringsToDictionaryByteArray(this Dictionary<byte[], byte[]> dict, string encryptedKey, string encryptedValue)
        {
            var listKeysStr = encryptedKey.Split('-');
            var listValuesStr = encryptedValue.Split("-");
            var byteKeys = listKeysStr.ToList().ConvertAll(item => Convert.FromBase64String(item));
            var byteValues = listValuesStr.ToList().ConvertAll(item => Convert.FromBase64String(item));
            for (var i = 0; i < byteKeys.Count; i++)
            {
                dict.Add(byteKeys[i], byteValues[i]);
            }
        }
    }
}
