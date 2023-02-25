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
using Google.Protobuf.Reflection;
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
            //Configuration options
            services.Configure<ServiceOptions>(configuration.GetSection("Employee:ConfigurationOptions"));
            services.Configure<List<ExternalValidEndpointOptions>>(configuration.GetSection(ExternalValidEndpointOptions.Identifier));

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
            services.AddSingleton<IMessageBusSender, ServiceBusMessageSender>();
            //services.AddHostedService<MessageSenderHostedService>();
            services.AddHostedService<EmployeeGenerator>();
            services.AddSingleton<IMessageSenderService, MessageSenderService>();
            services.Configure<DatabaseSettingOptions>(configuration.GetSection(DatabaseSettingOptions.DatabaseSetting));
            services.Configure<StorageLocationOptions>(configuration.GetSection(StorageLocationOptions.StorageLocation));
            services.Configure<WebHookConfigurationOptions>((option) => 
            {
                //the property name does not match then appsettings
                var subscribeToMessageIdentifiers = configuration.GetValue<string>("Employee:WebHookConfiguration:SubscribeToMessageIdentifiers");
                option.SubscribeToMessageIdentifiers = string.IsNullOrEmpty(subscribeToMessageIdentifiers) 
                                                                ? (new List<string>()).AsEnumerable()
                                                                : subscribeToMessageIdentifiers.Split(',').AsEnumerable();
                option.WebHookReceiveMessageUrl = configuration.GetValue<string>("Employee:WebHookConfiguration:WebHookReceiveMessageUrl");
                option.WebHookSendMessageUrl = configuration.GetValue<string>("Employee:WebHookConfiguration:WebHookSendMessageUrl");
                option.WebHookSubscriptionUrl = configuration.GetValue<string>("Employee:WebHookConfiguration:WebHookSubscriptionUrl");
            });
            services.Configure<WebHookRetryOptions>(option => 
            {
                option.TimeOut = TimeSpan.FromSeconds(configuration.GetValue<int>("Employee:WebHookConfiguration:RetryOptions:TimeOutInSeconds"));
                option.MaxRetries = configuration.GetValue<int>("Employee:WebHookConfiguration:RetryOptions:MaxRetries");
                option.Delay = TimeSpan.FromSeconds(configuration.GetValue<int>("Employee:WebHookConfiguration:RetryOptions:DelayInSeconds"));
            });
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
