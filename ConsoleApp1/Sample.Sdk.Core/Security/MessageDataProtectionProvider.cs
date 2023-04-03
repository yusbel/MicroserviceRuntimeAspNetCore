using Microsoft.Extensions.Logging;
using Sample.Sdk.Data.Enums;
using Sample.Sdk.Data.Security;
using Sample.Sdk.Interface;
using Sample.Sdk.Interface.Security;
using Sample.Sdk.Interface.Security.Asymetric;
using Sample.Sdk.Interface.Security.Symetric;
using System.Collections.Concurrent;
using System.Text;

namespace Sample.Sdk.Core.Security
{
    /// <summary>
    /// Encrypt decrypt message content at rest and in transit
    /// </summary>
    public class MessageDataProtectionProvider : IMessageDataProtectionProvider
    {
        private const string FirstEncryptionMsgKey = "FirstEncryptionMessageKey";
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

        #region Decrypt

        public async Task<List<KeyValuePair<string, string>>>
            UnProtect(List<KeyValuePair<SymetricResult, SymetricResult>> data,
            byte[] aad,
            CancellationToken token)
        {
            var keyDecryptionResult = await DecryptMessageKeys(data, FirstEncryptionMsgKey, token).ConfigureAwait(false);

            if (TryDecrypt(keyDecryptionResult, 1, aad, out var firstDecryptionResult))
            {
                firstDecryptionResult = firstDecryptionResult.ConvertAll(kvp =>
                {
                    kvp.Key.EncryptedData = kvp.Key.PlainData;
                    kvp.Value.EncryptedData = kvp.Value.PlainData;
                    return kvp;
                }).ToList();
                if (TryDecrypt(firstDecryptionResult, 0, aad, out var secondDecryptionResult))
                {
                    var plainDataResult = new List<KeyValuePair<string, string>>();
                    for (var i = 0; i < secondDecryptionResult.Count; i++)
                    {
                        plainDataResult.Add(KeyValuePair.Create(Encoding.UTF8.GetString(secondDecryptionResult[i].Key.PlainData),
                                                                Encoding.UTF8.GetString(secondDecryptionResult[i].Value.PlainData)));
                    }
                    return plainDataResult;
                }
            }
            throw new InvalidOperationException("Decryption fail");
        }

        private bool TryDecrypt(List<KeyValuePair<SymetricResult, SymetricResult>> data, int keyIndex, byte[] aad,
            out List<KeyValuePair<SymetricResult, SymetricResult>> result)
        {
            result = new List<KeyValuePair<SymetricResult, SymetricResult>>();
            var decryptResult = new ConcurrentBag<KeyValuePair<SymetricResult, SymetricResult>>();
            Parallel.ForEach(data, symetricKeyValue => 
            {
                if (_symetricCryptoProvider.TryDecrypt(symetricKeyValue.Key.EncryptedData,
                                                        symetricKeyValue.Key.Key[keyIndex],
                                                        symetricKeyValue.Key.Tag[keyIndex],
                                                        symetricKeyValue.Key.Nonce[keyIndex],
                                                        aad, out var symetricDecryptResult))
                {
                    if (_symetricCryptoProvider.TryDecrypt(symetricKeyValue.Value.EncryptedData,
                                                            symetricKeyValue.Value.Key[keyIndex],
                                                            symetricKeyValue.Value.Tag[keyIndex],
                                                            symetricKeyValue.Value.Nonce[keyIndex],
                                                            aad, out var symetricValueDecrypt))
                    {
                        symetricKeyValue.Key.PlainData = symetricDecryptResult!.PlainData;
                        symetricKeyValue.Value.PlainData = symetricValueDecrypt!.PlainData;
                        decryptResult.Add(KeyValuePair.Create(symetricKeyValue.Key, symetricKeyValue.Value));
                    }
                }
            });
            result = decryptResult.ToList();
            return data.Count == result.Count;
        }


        private async Task<List<KeyValuePair<SymetricResult, SymetricResult>>>
            DecryptMessageKeys(List<KeyValuePair<SymetricResult, SymetricResult>> encryptedKeys,
                            string certificateName,
                            CancellationToken token)
        {
            var decryptedKeys = new ConcurrentBag<KeyValuePair<SymetricResult, SymetricResult>>();
            //await Parallel.ForEachAsync(encryptedKeys, async (symetricResult, token) =>
            foreach (var symetricResult in encryptedKeys)
            {
                for (var i = 0; i < symetricResult.Key.Key.Count; i++)
                {
                    var keyDecryptResult = await _asymetricCryptoProvider.Decrypt(symetricResult.Key.Key[i],
                            Enums.HostTypeOptions.Runtime,
                            certificateName,
                            token)
                        .ConfigureAwait(false);
                    var valueDecryptResult = await _asymetricCryptoProvider.Decrypt(symetricResult.Value.Key[i],
                            Enums.HostTypeOptions.Runtime,
                            certificateName,
                            token)
                        .ConfigureAwait(false);

                    symetricResult.Key.Key[i] = keyDecryptResult.data!;
                    symetricResult.Value.Key[i] = valueDecryptResult.data!;
                }
                decryptedKeys.Add(KeyValuePair.Create(symetricResult.Key, symetricResult.Value));
            }//).ConfigureAwait(false);

            return decryptedKeys.ToList();
        }


        [Obsolete]
        private bool TryDecrypt(List<KeyValuePair<byte[], byte[]>> keys,
            Dictionary<byte[], byte[]> data,
            List<KeyValuePair<byte[], byte[]>> nonces,
            List<KeyValuePair<byte[], byte[]>> tags,
            byte[] aad,
            out Dictionary<SymetricResult, SymetricResult> decryptedData)
        {
            if (data.Keys.Count != nonces.Count || data.Keys.Count != tags.Count)
            {
                throw new InvalidOperationException();
            }
            if (data.Values.Count != nonces.Count || data.Keys.Count != tags.Count)
            {
                throw new InvalidOperationException();
            }
            decryptedData = new Dictionary<SymetricResult, SymetricResult>();
            var listDataOfKeys = data.Keys.ToList();
            var listDataOfValues = data.Values.ToList();
            var listKeyOfKeys = keys.Select(item => item.Key).ToList();
            var listValuesOfKeys = keys.Select(item => item.Value).ToList();
            var listNonceOfKey = nonces.Select(item => item.Key).ToList();
            var listNonceOfValues = nonces.Select(item => item.Value).ToList();
            var listTagOfKey = tags.Select(item => item.Key).ToList();
            var listTagOfValues = tags.Select(item => item.Value).ToList();

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
                        decryptedData.Add(keyResult!, valueResult!);
                    }
                }
            }
            return true;
        }

        #endregion

        #region Encrypt

        public async Task<List<KeyValuePair<SymetricResult, SymetricResult>>> Protect(
            List<KeyValuePair<SymetricResult, SymetricResult>> data,
            byte[] aad,
            CancellationToken token)
        {
            if (TryEncrypt(_serviceContext.GetAesKeys().ToList(), data, aad, out var firstEncryptionResult))
            {
                token.ThrowIfCancellationRequested();
                firstEncryptionResult = firstEncryptionResult.ConvertAll(item =>
                {
                    item.Key.PlainData = item.Key.EncryptedData;
                    item.Value.PlainData = item.Value.EncryptedData;
                    return item;
                }).ToList();
                //using random keys
                if (TryEncrypt(firstEncryptionResult, aad, out var doubleSymetricResult))
                {
                    List<KeyValuePair<SymetricResult, SymetricResult>> firstKeyEncryptionResult;
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        firstKeyEncryptionResult = await EncryptMessageKeys(doubleSymetricResult,
                                Enums.HostTypeOptions.Runtime,
                                FirstEncryptionMsgKey,
                                token)
                            .ConfigureAwait(false);

                        return firstKeyEncryptionResult;
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }
            }
            throw new InvalidOperationException("Encryption fail");
        }

        /// <summary>
        /// Use the random key in context
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="data"></param>
        /// <param name="aad"></param>
        /// <param name="msgProtectionResult"></param>
        /// <returns></returns>
        /// 
        private bool TryEncrypt(List<byte[]> keys, List<KeyValuePair<SymetricResult, SymetricResult>> data, byte[] aad,
            out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult)
        {
            var encryptedMessage = new ConcurrentBag<KeyValuePair<SymetricResult, SymetricResult>>();
            var keysConcurrentBag = new ConcurrentBag<byte[]>(keys);
            try
            {
                Parallel.ForEach(data, (msg) =>
                {
                    var rnd = new Random();
                    int index = rnd.Next(0, keysConcurrentBag.Count - 1);
                    var keyProperty = keysConcurrentBag.ElementAt(index);
                    if (_symetricCryptoProvider.TryEncrypt(keyProperty, msg.Key.PlainData, aad, out var symetricKeyResult))
                    {
                        var keyValue = keysConcurrentBag.ElementAt(rnd.Next(0, keysConcurrentBag.Count - 1));
                        if (_symetricCryptoProvider.TryEncrypt(keyValue, msg.Value.PlainData, aad, out var symetricValueResult))
                        {
                            msg.Key.Key.Add(symetricKeyResult!.Key.First());
                            msg.Key.Nonce.Add(symetricKeyResult!.Nonce.First());
                            msg.Key.Tag.Add(symetricKeyResult!.Tag.First());
                            msg.Key.EncryptedData = symetricKeyResult!.EncryptedData;

                            msg.Value.Key.Add(symetricValueResult!.Key.First());
                            msg.Value.Tag.Add(symetricValueResult!.Tag.First());
                            msg.Value.Nonce.Add(symetricValueResult!.Nonce.First());
                            msg.Value.EncryptedData = symetricValueResult!.EncryptedData;

                            encryptedMessage.Add(KeyValuePair.Create(msg.Key, msg.Value));
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

        [Obsolete]
        private bool TryEncrypt(List<KeyValuePair<SymetricResult, SymetricResult>> data, int keyIndex, byte[] aad,
                                out List<KeyValuePair<SymetricResult, SymetricResult>> encryptedSymetricResult)
        {
            encryptedSymetricResult = new List<KeyValuePair<SymetricResult, SymetricResult>>();
            foreach (var kvp in data)
            {
                if (_symetricCryptoProvider.TryEncrypt(kvp.Key.Key[keyIndex], kvp.Key.PlainData, aad,
                                                        out var keySymetricResult))
                {
                    if (_symetricCryptoProvider.TryEncrypt(kvp.Value.Key[keyIndex], kvp.Value.PlainData, aad,
                                                        out var valueSymetricResult))
                    {
                        kvp.Key.Nonce.Add(keySymetricResult!.Nonce.First());
                        kvp.Key.Tag.Add(keySymetricResult!.Tag.First());
                        kvp.Value.Nonce.Add(valueSymetricResult!.Nonce.First());
                        kvp.Value.Tag.Add(valueSymetricResult!.Tag!.First());

                        encryptedSymetricResult.Add(KeyValuePair.Create(
                            new SymetricResult()
                            {
                                EncryptedData = keySymetricResult!.EncryptedData,
                                Key = kvp.Key.Key,
                                Nonce = kvp.Key.Nonce,
                                Tag = kvp.Key.Tag,
                                Aad = kvp.Key.Aad,
                                PlainData = kvp.Key.PlainData
                            },
                            new SymetricResult()
                            {
                                EncryptedData = valueSymetricResult!.EncryptedData,
                                Nonce = kvp.Value.Nonce,
                                Tag = kvp.Value.Tag,
                                Key = kvp.Key.Key,
                                PlainData = kvp.Value.PlainData,
                                Aad = kvp.Value.Aad
                            }));
                    }
                }
            }
            return data.Count == encryptedSymetricResult.Count;
        }
        /// <summary>
        /// Use random key
        /// </summary>
        /// <param name="data"></param>
        /// <param name="aad"></param>
        /// <param name="msgProtectionResult"></param>
        /// <returns></returns>
        private bool TryEncrypt(List<KeyValuePair<SymetricResult, SymetricResult>> data, byte[] aad,
                            out List<KeyValuePair<SymetricResult, SymetricResult>> msgProtectionResult)
        {
            var result = new ConcurrentBag<KeyValuePair<SymetricResult, SymetricResult>>();
            Parallel.ForEach(data, (msg) =>
            {
                if (_symetricCryptoProvider.TryEncrypt(msg.Key.PlainData, aad, out var symetricKeyResult))
                {
                    if (_symetricCryptoProvider.TryEncrypt(msg.Value.PlainData, aad, out var symetricValueResult))
                    {
                        msg.Key.Key.Add(symetricKeyResult!.Key.First());
                        msg.Key.Nonce.Add(symetricKeyResult!.Nonce.First());
                        msg.Key.Tag.Add(symetricKeyResult.Tag.First());
                        msg.Key.EncryptedData = symetricKeyResult!.EncryptedData;

                        msg.Value.Key.Add(symetricValueResult!.Key.First());
                        msg.Value.Nonce.Add(symetricValueResult.Nonce.First());
                        msg.Value.Tag.Add(symetricValueResult!.Tag.First());
                        msg.Value.EncryptedData = symetricValueResult!.EncryptedData;

                        result.Add(KeyValuePair.Create(msg.Key, msg.Value));
                    }
                }
            });
            msgProtectionResult = result.ToList();
            return data.Count == msgProtectionResult.Count;
        }

        private async Task<List<KeyValuePair<SymetricResult, SymetricResult>>>
            EncryptMessageKeys(List<KeyValuePair<SymetricResult, SymetricResult>> keys,
                                Enums.HostTypeOptions keyVaultType,
                                string certificateName,
                               CancellationToken token)
        {
            var encryptedkeys = new ConcurrentBag<KeyValuePair<SymetricResult, SymetricResult>>();
            token.ThrowIfCancellationRequested();
            await Parallel.ForEachAsync(keys, async (symetricResult, token) => 
            //foreach (var symetricResult in keys)
            {
                for (var i = 0; i < symetricResult.Key.Key.Count; i++)
                {
                    var propertyKeyEncrypted = await _asymetricCryptoProvider.Encrypt(symetricResult.Key.Key[i],
                                                                                        keyVaultType,
                                                                                        certificateName,
                                                                                        token)
                                                                        .ConfigureAwait(false);
                    var valuePropKeyEncrypted = await _asymetricCryptoProvider.Encrypt(symetricResult.Value.Key[i],
                                                                                        keyVaultType,
                                                                                        certificateName,
                                                                                        token)
                                                                        .ConfigureAwait(false);
                    symetricResult.Key.Key[i] = propertyKeyEncrypted.data!;
                    symetricResult.Value.Key[i] = valuePropKeyEncrypted.data!;
                }
                encryptedkeys.Add(KeyValuePair.Create(symetricResult.Key, symetricResult.Value));
            }).ConfigureAwait(false);
            return encryptedkeys.ToList();
        }

        #endregion
    }
}
