namespace SampleSdkRuntime.AzureAdmin.BlobLibs
{
    public interface IBlobProvider
    {
        Task<bool> UploadSignaturePublicKey(string certificateNameConfigKey, CancellationToken token);
        Task<byte[]> DownloadSignaturePublicKey(string certificateNameConfigKey, CancellationToken token);
    }
}