using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Azure;
using Azure.Messaging.ServiceBus;
using Sample.PayRoll.Messages.InComming;
using Sample.PayRoll.Entities;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.DatabaseContext;
using Sample.PayRoll.Services;
using Sample.Sdk.Core.Security;
using Sample.PayRoll.Services.Processors.Converter;
using Sample.Sdk.Services.Interfaces;
using Sample.PayRoll.Messages.InComming.Services;
using Sample.Sdk;

namespace Sample.PayRoll
{
    public static class PayRollServiceDependencyExtensions
    {
        public static IServiceCollection AddPayRollServiceDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IMessageConverter<EmployeeDto>, EmployeeAddedConverter>();
            services.AddTransient<IPayRoll, PayRoll>();
            services.AddTransient<IEntityContext<PayRollContext, PayRollEntity>, EntityContext<PayRollContext, PayRollEntity>>();
            
            //Configuration Options
            services.Configure<List<ExternalValidEndpointOptions>>(configuration.GetSection(ExternalValidEndpointOptions.SERVICE_SECURITY_VALD_ENDPOINTS_ID));

            services.AddHttpClient();//TODO:setup polly

            services.AddDbContext<PayRollContext>(options =>
            {
                options.EnableDetailedErrors(true);
            });
            //Adding sdk dependecies
            services.AddSampleSdk(configuration);
            services.AddSampleSdkInMemoryServices(configuration);
            services.AddSampleSdkDataProtection(configuration, configuration.GetValue<string>("ServiceSdk:Security:AzureKeyVaultOptions"));
            return services;
        }
    }
}
