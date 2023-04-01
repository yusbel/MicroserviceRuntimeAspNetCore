using Sample.Sdk.Data.Msg;

namespace Sample.EmployeeSubdomain.Services.Interfaces
{
    public interface IMessageSenderService
    {
        public Task<bool> Send(CancellationToken token);
        public Task<bool> Acknowledgement(ExternalMessage externalMsg, CancellationToken token);
    }
}