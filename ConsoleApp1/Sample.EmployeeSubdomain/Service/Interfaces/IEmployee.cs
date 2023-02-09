using Sample.EmployeeSubdomain.Service.Entities;

namespace Sample.EmployeeSubdomain.Service.Interfaces
{
    public interface IEmployee
    {
        Task<EmployeeEntity> CreateAndSave(string name, string email);
        Task<EmployeeEntity> GetEmployee(Guid id);
    }
}