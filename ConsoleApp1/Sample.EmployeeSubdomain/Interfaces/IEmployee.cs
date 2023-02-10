using Sample.EmployeeSubdomain.Entities;

namespace Sample.EmployeeSubdomain.Interfaces
{
    public interface IEmployee
    {
        Task<EmployeeEntity> CreateAndSave(string name, string email);
        Task<EmployeeEntity> GetEmployee(Guid id);
    }
}