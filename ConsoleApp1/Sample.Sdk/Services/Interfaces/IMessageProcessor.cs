using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Interfaces
{
    public interface IMessageProcessor<T> where T : class
    {
        public Task<T> Process(CancellationToken token, ExternalMessage message);
    }
}
