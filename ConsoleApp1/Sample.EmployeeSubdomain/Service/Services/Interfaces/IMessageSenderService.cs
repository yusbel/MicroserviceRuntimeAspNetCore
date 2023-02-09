namespace Sample.EmployeeSubdomain.Service.Services.Interfaces
{
    public interface IMessageSenderService
    {
        public Task<bool> Send(CancellationToken token, bool delete = false);
    }
}