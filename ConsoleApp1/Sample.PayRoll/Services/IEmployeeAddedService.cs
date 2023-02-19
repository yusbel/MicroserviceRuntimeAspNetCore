namespace Sample.PayRoll.Services
{
    public interface IEmployeeAddedService
    {
        Task<bool> Process(CancellationToken token);
    }
}