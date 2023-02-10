using Microsoft.Extensions.Hosting;
using Sample.EmployeeSubdomain.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Services
{
    public class MessageSenderHostedService : IHostedService
    {
        private readonly IMessageSenderService _messageSenderService;

        public MessageSenderHostedService(IMessageSenderService messageSenderService)
        {
            _messageSenderService = messageSenderService;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _messageSenderService.Send(cancellationToken, true);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
