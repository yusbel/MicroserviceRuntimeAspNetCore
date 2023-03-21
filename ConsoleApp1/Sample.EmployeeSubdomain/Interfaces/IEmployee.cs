using Sample.EmployeeSubdomain.Entities;

namespace Sample.EmployeeSubdomain.Interfaces
{
    public interface IEmployee
    {
        Task<EmployeeEntity> CreateAndSave(string name, string email, CancellationToken token);
        EmployeeEntity? GetEntity();
        Task Load(Guid employeeId, CancellationToken token);
    }
}