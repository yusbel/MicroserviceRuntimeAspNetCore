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
            services.Configure<DatabaseSettingOptions>(configuration.GetSection(DatabaseSettingOptions.DatabaseSetting));
            services.Configure<StorageLocationOptions>(configuration.GetSection(StorageLocationOptions.StorageLocation));
            services.Configure<WebHookConfigurationOptions>(configuration.GetSection(WebHookConfigurationOptions.SERVICE_WEBHOOK_CONFIG_OPTIONS_SECTION_ID));
            services.Configure<WebHookRetryOptions>(configuration.GetSection(WebHookRetryOptions.SERVICE_WEBHOOK_RETRY_OPTIONS_SECTION_ID));
            services.AddAzureClients(azureClientFactoryBuilder =>
            {
                var serviceBusConnStr = configuration.GetValue<string>("Employee:AzureServiceBusInfo:DefaultConnStr");
                //For employee message service to send and retrieve employee messages
                azureClientFactoryBuilder.AddServiceBusClient(serviceBusConnStr)
                //.WithName("ServiceBusClientEmployeeMessages")
                .ConfigureOptions((options, host) =>
                {
                    options.Identifier = "ServiceBusClientEmployeeMessages";
                    options.RetryOptions = new ServiceBusRetryOptions()
                    {
                        Delay = TimeSpan.FromSeconds(configuration.GetValue<int>("Employee:Service:Default:RetryOptions:DelayInSeconds")),
                        MaxDelay = TimeSpan.FromSeconds(configuration.GetValue<int>("Employee:Service:Default:RetryOptions:MaxDelayInSeconds")),
                        MaxRetries = configuration.GetValue<int>("Employee:Service:Default:RetryOptions:MaxRetries"),
                        Mode = configuration.GetValue<string>("Employee:Service:Default:RetryOptions:Mode") == "Fixed" ? ServiceBusRetryMode.Fixed 
                                                                                                                       : ServiceBusRetryMode.Exponential
                    };
                });
            });
            return services;
        }
    }
}
