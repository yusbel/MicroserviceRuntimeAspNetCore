namespace Sample.Sdk.Interface.Security
{
    public interface ISecurityEndpointValidator
    {
        bool IsDecryptEndpointValid(string endpoint);
        bool IsWellKnownEndpointValid(string endpoint);

        bool IsAcknowledgementValid(string endpoint);
        bool IsMessageEndpointValid(string messageEndpoint, string connectionEndpoint);
    }
}