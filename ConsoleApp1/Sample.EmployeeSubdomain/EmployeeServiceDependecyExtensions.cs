using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sample.Sdk.Persistance.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Azure;
using Azure.Messaging.ServiceBus;
using Sample.EmployeeSubdomain.Services.Interfaces;
using Sample.EmployeeSubdomain.Services;
using Sample.EmployeeSubdomain.Entities;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Settings;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.EmployeeSubdomain.WebHook.Data;
using Sample.Sdk.Core.Security.Providers.Asymetric.Interfaces;
using Sample.Sdk.Core.Security.Providers.Asymetric;
using Sample.Sdk.InMemory;
using Sample.Sdk.Core.Security.Providers.Protocol.State;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Sample.Sdk.Core.Security.Providers.Protocol;
using Sample.Sdk.Core.Security;
using Sample.Sdk.AspNetCore.Middleware;
using Sample.EmployeeSubdomain.Messages.Acknowledgement;
using Sample.Sdk;
using Sample.Sdk.Core.EntityDatabaseContext;

namespace Sample.EmployeeSubdomain
{
    public static class EmployeeServiceDependecyExtensions
    {
        public static IServiceCollection AddEmployeeServiceDependencies(this IServiceCollection services, IConfiguration configuration) 
        {
            services.Configure<List<ExternalValidEndpointOptions>>(configuration.GetSection(ExternalValidEndpointOptions.SERVICE_SECURITY_VALD_ENDPOINTS_ID));

            //Security
            services.AddTransient<IProcessAcknowledgement, MessageProcessAcknowledgement>();
            services.AddSingleton<IInMemoryMessageBus<ShortLivedSessionState>, InMemoryMessageBus<ShortLivedSessionState>>();
            services.AddSingleton<IInMemoryMessageBus<PointToPointChannel>, InMemoryMessageBus<PointToPointChannel>>();
            
            services.AddTransient<IEmployee, Employee>();
            services.AddTransient<IEntityContext<EmployeeContext, EmployeeEntity>, EntityContext<EmployeeContext, EmployeeEntity>>();
            services.AddDbContext<EmployeeContext>(options =>
            {
                options.EnableDetailedErrors(true);
            });
            services.AddSingleton<IMessageSender, ServiceBusMessageSender>();
            //services.AddHostedService<MessageSenderHostedService>();
            services.AddHostedService<EmployeeGenerator>();
            services.AddSingleton<IMessageSenderService, MessageSenderService>();
            services.Configure<StorageLocationOptions>(configuration.GetSection(StorageLocationOptions.StorageLocation));
            services.Configure<WebHookConfigurationOptions>(configuration.GetSection(WebHookConfigurationOptions.SERVICE_WEBHOOK_CONFIG_OPTIONS_SECTION_ID));
            services.Configure<WebHookRetryOptions>(configuration.GetSection(WebHookRetryOptions.SERVICE_WEBHOOK_RETRY_OPTIONS_SECTION_ID));
            
            //Adding sdk dependecies
            services.AddSampleSdk(configuration);

            return services;
        }
    }
}
