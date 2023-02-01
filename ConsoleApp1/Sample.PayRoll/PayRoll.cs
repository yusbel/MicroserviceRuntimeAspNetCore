using Microsoft.Extensions.Logging;
using Sample.PayRoll.Payroll.DatabaseContext;
using Sample.PayRoll.Payroll.Entities;
using Sample.PayRoll.Payroll.Interfaces;
using Sample.PayRoll.Payroll.Messages.InComming;
using Sample.Sdk.Core;
using Sample.Sdk.Persistance;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll
{
    public class PayRoll : PersistenceObject<PayRoll, PayRollContext, PayRollEntity>, IPayRoll
    {
        public PayRoll(ILoggerFactory logger, IEntityContext<PayRollContext, PayRollEntity> entityContext) : base(logger.CreateLogger<PayRoll>(), entityContext)
        {
        }
        protected override void AttachEntity(PayRollEntity entity) => _payRollEntity = entity;
        private PayRollEntity? _payRollEntity;
        protected override PayRollEntity GetInMemoryEntity() => _payRollEntity ?? new PayRollEntity();

        public async Task<bool> CreatePayRoll(string employeeIdentifier, decimal monthlySalary, bool sendMail)
        {
            _payRollEntity = new PayRollEntity() { EmployeeIdentifier = employeeIdentifier, MonthlySalary = monthlySalary, MailPaperRecord = sendMail, Id = Guid.NewGuid() };
            await Save(() => StaticBaseObject<EmployeeAdded>.Notify(new EmployeeAdded()));
            return true;
        }
    }
}
