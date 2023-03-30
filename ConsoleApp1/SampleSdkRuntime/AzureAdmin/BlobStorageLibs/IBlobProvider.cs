namespace SampleSdkRuntime.AzureAdmin.BlobStorageLibs
{
    public interface IBlobProvider
    {
        Task<bool> UploadPublicKey(string certificateNameConfigKey, CancellationToken token);
    }
}