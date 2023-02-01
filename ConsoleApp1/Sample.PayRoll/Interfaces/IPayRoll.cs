namespace Sample.PayRoll.Payroll.Interfaces
{
    public interface IPayRoll
    {
        Task<bool> CreatePayRoll(string employeeIdentifier, decimal monthlySalary, bool sendMail);
    }
}