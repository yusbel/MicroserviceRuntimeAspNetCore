using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Extensions;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Symetric;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Core.Security.DataProtection
{
    /// <summary>
    /// Encrypt decrypt message content at rest and in transit
    /// </summary>
    public class MessageDataProtectionProvider : IMessageDataProtectionProvider
    {
        private readonly IServiceContext _serviceContext;
        private readonly ISymetricCryptoProvider _symetricCryptoProvider;
        private readonly ILogger<MessageDataProtectionProvider> _logger;
        private readonly IAsymetricCryptoProvider _asymetricCryptoProvider;

        public MessageDataProtectionProvider(IServiceContext serviceContext,
            ISymetricCryptoProvider symetricCryptoProvider,
            ILogger<MessageDataProtectionProvider> logger,
            IAsymetricCryptoProvider asymetricCryptoProvider)
        {
            _serviceContext = serviceContext;
            _symetricCryptoProvider = symetricCryptoProvider;
            _logger = logger;
            _asymetricCryptoProvider = asymetricCryptoProvider;
        }

        /// <summary>
        /// Use the random key in context
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="data"></param>
        /// <param name="aad"></param>
        /// <param name="msgProtectionResult"></param>
        /// <returns></returns>
        public bool TryEncrypt(List<byte[]> keys, Dictionary<byte[], byte[]> data, byte[] aad, 
            out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult)
        {
            var encryptedMessage = new ConcurrentDictionary<SymetricResult, SymetricResult>();
            var concurrentBag = new ConcurrentBag<byte[]>(keys);
            try
            {
                Parallel.ForEach(data, (msg) =>
                {
                    var rnd = new Random();
                    int index = rnd.Next(0, concurrentBag.Count - 1);
                    var keyProperty = concurrentBag.ElementAt(index);
                    if (_symetricCryptoProvider.TryEncrypt(keyProperty, msg.Key, aad, out var symetricKeyResult))
                    {
                        var keyValue = concurrentBag.ElementAt(rnd.Next(0, concurrentBag.Count - 1));
                        if (_symetricCryptoProvider.TryEncrypt(keyValue, msg.Value, aad, out var symetricValueResult))
                        {
                            encryptedMessage.TryAdd(symetricKeyResult, symetricValueResult);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                throw;
            }
            msgProtectionResult = encryptedMessage.ToList();
            return data.Count == msgProtectionResult.Count;
        }

        public bool TryDecrypt(Dictionary<byte[], byte[]> keys, 
            Dictionary<byte[], byte[]> data, 
            Dictionary<byte[], byte[]> nonces, 
            Dictionary<byte[], byte[]> tags,
            byte[] aad,
            out Dictionary<SymetricResult, SymetricResult> decryptedData) 
        {
            if (data.Keys.Count != nonces.Keys.Count || data.Keys.Count != tags.Keys.Count) 
            {
                throw new InvalidOperationException();
            }
            if(data.Values.Count != nonces.Values.Count || data.Keys.Count != tags.Values.Count) 
            {
                throw new InvalidOperationException();
            }
            decryptedData = new Dictionary<SymetricResult, SymetricResult>();
            var listDataOfKeys = data.Keys.ToList();
            var listDataOfValues = data.Values.ToList();
            var listKeyOfKeys = keys.Keys.ToList();
            var listValuesOfKeys = keys.Values.ToList();
            var listNonceOfKey = nonces.Keys.ToList();
            var listNonceOfValues = nonces.Values.ToList();
            var listTagOfKey = keys.Keys.ToList();
            var listTagOfValues = keys.Values.ToList();

            for (var i = 0; i < listDataOfKeys.Count; i++) 
            {
                if (_symetricCryptoProvider.TryDecrypt(listDataOfKeys[i], 
                                                        listKeyOfKeys[i], 
                                                        listTagOfKey[i], 
                                                        listNonceOfKey[i], aad, out var keyResult))
                {
                    if (_symetricCryptoProvider.TryDecrypt(listDataOfValues[i],
                                                            listValuesOfKeys[i],
                                                            listTagOfValues[i],
                                                            listNonceOfValues[i], aad, out var valueResult)) 
                    {
                        decryptedData.Add(keyResult, valueResult);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Use random key
        /// </summary>
        /// <param name="data"></param>
        /// <param name="aad"></param>
        /// <param name="msgProtectionResult"></param>
        /// <returns></returns>
        public bool TryEncrypt(Dictionary<byte[], byte[]> data, byte[] aad, 
                            out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult) 
        {
            var result = new ConcurrentDictionary<SymetricResult, SymetricResult>();
            Parallel.ForEach(data, (msg) => 
            {
                if (_symetricCryptoProvider.TryEncrypt(msg.Key, aad, out var symetricKeyResult)) 
                {
                    if (_symetricCryptoProvider.TryEncrypt(msg.Value, aad, out var symetricValueResult)) 
                    {
                        result.TryAdd(symetricKeyResult, symetricValueResult);
                    }
                }
            });
            msgProtectionResult = result.ToList();
            return true;
        }

        public async Task<Dictionary<byte[], byte[]>> EncryptMessageKeys(List<KeyValuePair<byte[], byte[]>> keys, CancellationToken token)
        {
            var encryptedkeys = new Dictionary<byte[], byte[]>();
            foreach (var key in keys)
            {   
                var propertyKeyEncrypted = await _asymetricCryptoProvider.Encrypt(key.Key, token).ConfigureAwait(false);
                var valuePropKeyEncrypted = await _asymetricCryptoProvider.Encrypt(key.Value, token).ConfigureAwait(false);
                encryptedkeys.Add(propertyKeyEncrypted.data, valuePropKeyEncrypted.data);
            }
            return encryptedkeys;
        }

        public async Task<Dictionary<byte[], byte[]>> DecryptMessageKeys(Dictionary<byte[], byte[]> encryptedKeys, CancellationToken token) 
        {
            var decryptedKeys = new Dictionary<byte[], byte[]>();
            foreach (var key in encryptedKeys) 
            {
                var decryptedKey = await _asymetricCryptoProvider.Decrypt(key.Key, token).ConfigureAwait(false);
                var decryptedValue = await _asymetricCryptoProvider.Decrypt(key.Value, token).ConfigureAwait(false);
                decryptedKeys.Add(decryptedKey.data, decryptedValue.data);
            }
            return decryptedKeys;
        }

        private static List<byte[]> ExtractListToEncrypt(List<byte[]> listPropKeys)
        {
            var index = 0;
            var list = new List<byte[]>();
            foreach (var item in listPropKeys)
            {
                var currentLength = item.Length;
                while (currentLength > 0)
                {
                    byte[] array = list.Count == 0 || list.Last().GetIndexByteArray(0) == -1 ? new byte[32] : list.Last();
                    var startIndex = array.GetIndexByteArray(0);
                    var itemsToCopy = item.Skip(index).Take(array.Length - startIndex);
                    itemsToCopy.ToArray().CopyTo(array, startIndex);
                    list.Add(array);
                    currentLength -= array.Length - startIndex;
                    index += array.Length - startIndex;
                }
            }

            return list;
        }
    }
}
