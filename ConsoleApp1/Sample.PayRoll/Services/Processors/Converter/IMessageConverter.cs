using Sample.Sdk.Data.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services.Processors.Converter
{
    public interface IMessageConverter<TDto> where TDto : class 
    {
        public TDto Convert(ExternalMessage em);
    }
}
