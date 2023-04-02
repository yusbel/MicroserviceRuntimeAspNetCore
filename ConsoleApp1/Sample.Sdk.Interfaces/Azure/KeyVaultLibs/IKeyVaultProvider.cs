using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;

namespace Sample.Sdk.Interface.Azure.KeyVaultLibs
{
    public interface IKeyVaultProvider : IKeyVaultPolicyProvider
    {
        Task<(bool wasSaved, KeyVaultKey keyVaultKey)>
            CreateOrDeleteKeyInKeyVaultWithRetry(string keyName,
                                    KeyType keyType,
                                    CreateKeyOptions keyOptions,
                                    CancellationToken token,
                                    Func<string, KeyType, CreateKeyOptions, Task<KeyVaultKey>> createOrDeleteKey,
                                    int counter = 0,
                                    int maxRetry = 3);

        Task<(bool WasSaved, KeyVaultSecret? Secret)>
            SaveOrDeleteSecretInKeyVaultWithRetry(string secretKey,
                            string secretText,
                            Func<string, string, Task<KeyVaultSecret>> saveOrDeleteSecret,
                            CancellationToken cancellationToken,
                            int counter = 0,
                            int maxRetry = 3);

        Task<(bool wasSaved, KeyVaultKey keyVaultKey)>
           CreateOrDeleteOctKeyWithRetry(CreateOctKeyOptions keyOptions,
                                   CancellationToken token,
                                   Func<CreateOctKeyOptions, CancellationToken, Task<KeyVaultKey>> createOrDeleteKey,
                                   int counter = 0,
                                   int maxRetry = 3);

        Task<(bool wasSaved, KeyVaultKey keyVaultKey)>
           CreateOrDeleteKeyWithRetry(CreateKeyOptions keyOptions,
                                   CancellationToken token,
                                   Func<CreateKeyOptions, CancellationToken, Task<KeyVaultKey>> createOrDeleteKey,
                                   int counter = 0,
                                   int maxRetry = 3);

    }
}