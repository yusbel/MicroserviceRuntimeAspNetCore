using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.EmployeeSubdomain.Entities;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Messages;
using Sample.Sdk;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
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
    public class Employee : PersistenceObject<Employee, EmployeeContext, EmployeeEntity>, IEmployee
    {
        private ILogger<Employee> _logger;
        public Employee(ILoggerFactory loggerFactory,
            IEntityContext<EmployeeContext, EmployeeEntity> entityContext,
            IAsymetricCryptoProvider asymetricCryptoProvider,
            ISymetricCryptoProvider cryptoProvider,
            IOptions<CustomProtocolOptions> options,
            IMessageBusSender messageSender) : base(loggerFactory.CreateLogger<Employee>()
                , cryptoProvider
                , asymetricCryptoProvider
                , entityContext
                , options
                , messageSender)
        {
            Guard.ThrowWhenNull(entityContext, loggerFactory, messageSender);
            _logger = loggerFactory.CreateLogger<Employee>();
        }
        public async Task<EmployeeEntity> CreateAndSave(string name, string email, CancellationToken token)
        {
            _employee = new EmployeeEntity { Name = name, Email = email };
            try
            {
                await Save(new EmployeeAdded()
                {
                    Key = _employee.Id.ToString(),
                    CorrelationId = _employee.Id.ToString(),
                    Content = System.Text.Json.JsonSerializer.Serialize(_employee)
                }, token, sendNotification: true).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                _logger.LogCritical(e, "Creating and saving raised an error");
            }
            return _employee;
        }
        public async Task<EmployeeEntity> GetEmployee(Guid id, CancellationToken token)
        {
            return await GetEntityById(id, token);
        }
        protected override EmployeeEntity? GetInMemoryEntity() => _employee;
        protected override void AttachEntity(EmployeeEntity entity) => _employee = entity;
        private EmployeeEntity? _employee;

    }
}
