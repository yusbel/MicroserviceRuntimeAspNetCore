﻿using Microsoft.Extensions.Logging;
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
    public class PayRoll : PersistenceObject<PayRollContext, PayRollEntity>, IPayRoll
    {
        public PayRoll(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
        protected override void AttachEntity(PayRollEntity entity) => _payRollEntity = entity;
        private PayRollEntity? _payRollEntity;
        public override PayRollEntity? GetEntity() => _payRollEntity ?? new PayRollEntity();

        public async Task<bool> CreatePayRoll(string employeeIdentifier, decimal monthlySalary, bool sendMail, CancellationToken token)
        {
            _payRollEntity = new PayRollEntity() { EmployeeIdentifier = employeeIdentifier, MonthlySalary = monthlySalary, MailPaperRecord = sendMail, Id = Guid.NewGuid().ToString() };
            await Save(token);
            return true;
        }
    }
}
