using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.EmployeeSubdomain.Entities;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Messages;
using Sample.Sdk;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Exceptions;
using Sample.Sdk.Core.Security.Interfaces;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Persistance;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain
{
    /// <summary>
    /// Encapsulate employee requirements and use services to implement them. A persistance ignorance object.
    /// </summary>
    public class Employee : PersistenceObject<EmployeeContext, EmployeeEntity>, IEmployee
    {
        private ILogger<Employee> _logger;
        public Employee(ILogger<Employee> logger,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _logger = logger;
   ;
        }
        public async Task<EmployeeEntity> CreateAndSave(string name, string email, CancellationToken token)
        {
            _employee = new EmployeeEntity { Name = name, Email = email };
            try
            {
                var msg = EmployeeAdded.Create(_employee.Name, _employee.Email);
                await Save(msg, token, sendNotification: true).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception) { throw; }
            return _employee;
        }
        public override EmployeeEntity GetEntity() => _employee;
        protected override void AttachEntity(EmployeeEntity entity) => _employee = entity;
        private EmployeeEntity? _employee;

    }
}
