using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Employee.DatabaseContext;
using Sample.EmployeeSubdomain.Employee.Entities;
using Sample.EmployeeSubdomain.Employee.Interfaces;
using Sample.EmployeeSubdomain.Employee.Messages;
using Sample.Sdk.Core;
using Sample.Sdk.Persistance;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee
{
    public class Employee : PersistenceObject<Employee, EmployeeContext, EmployeeEntity>, IEmployee
    {
        private ILogger<Employee> _logger;
        public Employee(ILoggerFactory loggerFactory, IEntityContext<EmployeeContext, EmployeeEntity> entityContext) : 
            base(loggerFactory.CreateLogger<Employee>(), entityContext)
        {
            _logger = loggerFactory.CreateLogger<Employee>();
            _logger.LogInformation($"Employee constructor, is entity context {entityContext == null}");

        }
        public async Task<EmployeeEntity> CreateAndSave(string name, string email)
        {
            _employee = new EmployeeEntity { Name = name, Email = email };
            await Save(()=> StaticBaseObject<EmployeeAdded>.Notify(new EmployeeAdded()));
            return _employee;
        }
        public async Task<EmployeeEntity> GetEmployee(Guid id)
        {
            return await GetEntityById(id);
        }
        protected override EmployeeEntity GetInMemoryEntity() => _employee;
        protected override void AttachEntity(EmployeeEntity entity) => _employee = entity;
        private EmployeeEntity _employee;
    }
}
