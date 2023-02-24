using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sample.PayRoll.DatabaseContext;
using Sample.PayRoll.Entities;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.Messages.InComming;
using Sample.PayRoll.Messages.Sent;
using Sample.Sdk.Core;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security.Providers.Symetric.Interface;
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
        public PayRoll(ILoggerFactory logger
            , IEntityContext<PayRollContext, PayRollEntity> entityContext
            , IMessageBusSender messageDurable
            , IAsymetricCryptoProvider asymetricCryptoProvider
            , ISymetricCryptoProvider cryptoProvider
            , IOptions<CustomProtocolOptions> options) : base(logger.CreateLogger<PayRoll>()
                , cryptoProvider
                , asymetricCryptoProvider
                , entityContext
                , options
                , messageDurable)
        {
        }
        protected override void AttachEntity(PayRollEntity entity) => _payRollEntity = entity;
        private PayRollEntity? _payRollEntity;
        protected override PayRollEntity? GetInMemoryEntity() => _payRollEntity ?? new PayRollEntity();

        public async Task<bool> CreatePayRoll(string employeeIdentifier, decimal monthlySalary, bool sendMail, CancellationToken token)
        {
            _payRollEntity = new PayRollEntity() { EmployeeIdentifier = employeeIdentifier, MonthlySalary = monthlySalary, MailPaperRecord = sendMail, Id = Guid.NewGuid().ToString() };
            await Save(token);
            return true;
        }
    }
}
