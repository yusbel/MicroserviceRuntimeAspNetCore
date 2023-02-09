using Microsoft.Extensions.Logging;
using Sample.PayRoll.DatabaseContext;
using Sample.PayRoll.Entities;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.Messages.InComming;
using Sample.Sdk.Core;
using Sample.Sdk.Msg.Interfaces;
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
        public PayRoll(ILoggerFactory logger, IEntityContext<PayRollContext, PayRollEntity> entityContext, IMessageBusSender messageDurable) : base(logger.CreateLogger<PayRoll>(), entityContext, messageDurable)
        {
        }
        protected override void AttachEntity(PayRollEntity entity) => _payRollEntity = entity;
        private PayRollEntity? _payRollEntity;
        protected override PayRollEntity GetInMemoryEntity() => _payRollEntity ?? new PayRollEntity();

        public async Task<bool> CreatePayRoll(string employeeIdentifier, decimal monthlySalary, bool sendMail)
        {
            _payRollEntity = new PayRollEntity() { EmployeeIdentifier = employeeIdentifier, MonthlySalary = monthlySalary, MailPaperRecord = sendMail, Id = Guid.NewGuid() };
            await Save(null, () => StaticBaseObject<EmployeeAdded>.Notify(new EmployeeAdded()));
            return true;
        }
    }
}
