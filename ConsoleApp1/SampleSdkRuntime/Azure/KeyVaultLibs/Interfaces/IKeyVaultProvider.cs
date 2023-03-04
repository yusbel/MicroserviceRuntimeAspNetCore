namespace SampleSdkRuntime.Azure.KeyVaultLibs.Interfaces
{
    public interface IKeyVaultProvider : IKeyVaultPolicyProvider
    {
        Task<bool> SaveSecretInKeyVault(string secretKey, string secretText, int counter, CancellationToken cancellationToken);
    }
}