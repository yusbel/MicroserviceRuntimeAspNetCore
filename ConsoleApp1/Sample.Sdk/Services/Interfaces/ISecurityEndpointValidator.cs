namespace Sample.Sdk.Services.Interfaces
{
    public interface ISecurityEndpointValidator
    {
        bool IsDecryptEndpointValid(string endpoint);
        bool IsWellKnownEndpointValid(string endpoint);

        bool IsAcknowledgementValid(string endpoint);
    }
}