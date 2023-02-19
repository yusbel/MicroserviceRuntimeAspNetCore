using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Sample.Sdk.Msg.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.PayRoll.Services.Processors.Converter
{
    /// <summary>
    /// It could be generic for sdk
    /// </summary>
    public class EmployeeAddedConverter : IMessageConverter<EmployeeDto>
    {
        public EmployeeDto Convert(ExternalMessage em) 
        {
            return System.Text.Json.JsonSerializer.Deserialize<EmployeeDto>(em.Content);
        }
    }
}
