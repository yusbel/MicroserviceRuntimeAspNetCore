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

namespace Sample.EmployeeSubdomain
{
    public static class EmployeeServiceDependecyExtensions
    {
        public static IServiceCollection AddEmployeeServiceDependencies(this IServiceCollection services, IConfiguration configuration) 
        {
            services.AddTransient<IEmployee, Employee>();
            services.AddTransient<IEntityContext<EmployeeContext, EmployeeEntity>, EntityContext<EmployeeContext, EmployeeEntity>>();
            services.AddDbContext<EmployeeContext>(options =>
            {
                options.EnableDetailedErrors(true);
            });
            services.AddSingleton<IMessageBusSender, ServiceBusMessageSender>();
            services.AddHostedService<MessageSenderHostedService>();
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
                azureClientFactoryBuilder.AddServiceBusClient(serviceBusConnStr).ConfigureOptions((options, host) =>
                {
                    options.Identifier = "EmployeeService";
                    options.RetryOptions = new ServiceBusRetryOptions()
                    {
                        Delay = TimeSpan.FromSeconds(configuration.GetValue<int>("Employee:MessageBusInfo:EmployeeAddedSender:DelayInSeconds")),
                        MaxDelay = TimeSpan.FromSeconds(configuration.GetValue<int>("Employee:MessageBusInfo:EmployeeAddedSender:MaxDelayInSeconds")),
                        MaxRetries = configuration.GetValue<int>("Employee:MessageBusInfo:EmployeeAddedSender:MaxRetries"),
                        Mode = configuration.GetValue<string>("Employee:MessageBusInfo:EmployeeAddedSender:Mode") == "Fixed" ? ServiceBusRetryMode.Fixed : ServiceBusRetryMode.Exponential
                    };
                });
            });
            return services;
        }
    }
}
