using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services.gRPC
{
    public class PayRollService : PayRollRetrival.PayRollRetrivalBase
    {
        private ILogger<PayRollService> _logger;
        public PayRollService(ILogger<PayRollService> logger) => _logger = logger;
        public override Task<PayRollReply> GetPayRoll(PayRollRequest request, ServerCallContext context)
        {
            return Task.FromResult(new PayRollReply() { PayRollIdentifier = Guid.NewGuid().ToString() });
        }
    }
}
