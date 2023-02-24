namespace Sample.PayRoll.Interfaces
{
    public interface IPayRoll
    {
        Task<bool> CreatePayRoll(string employeeIdentifier, decimal monthlySalary, bool sendMail, CancellationToken token);
    }
}