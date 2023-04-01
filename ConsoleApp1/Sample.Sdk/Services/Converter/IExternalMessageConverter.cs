using Sample.Sdk.Data.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk.Services.Converter
{
    public interface IExternalMessageConverter<TOutput> 
    {
        Task<TOutput> Convert(ExternalMessage externalMessage);
    }
}
