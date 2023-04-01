namespace Sample.Sdk.Security.Interfaces
{
    public interface ISecurityEndpointValidator
    {
        bool IsDecryptEndpointValid(string endpoint);
        bool IsWellKnownEndpointValid(string endpoint);

        bool IsAcknowledgementValid(string endpoint);
        bool IsMessageEndpointValid(string messageEndpoint, string connectionEndpoint);
    }
}