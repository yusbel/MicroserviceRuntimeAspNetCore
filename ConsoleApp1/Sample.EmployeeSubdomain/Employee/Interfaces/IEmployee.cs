using Sample.EmployeeSubdomain.Employee.Entities;

namespace Sample.EmployeeSubdomain.Employee.Interfaces
{
    public interface IEmployee
    {
        Task<EmployeeEntity> CreateAndSave(string name, string email);
        Task<EmployeeEntity> GetEmployee(Guid id);
    }
}