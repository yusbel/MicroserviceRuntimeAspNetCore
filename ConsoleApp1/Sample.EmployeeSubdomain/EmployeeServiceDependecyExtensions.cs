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
using Sample.EmployeeSubdomain.Service.Services;
using Sample.EmployeeSubdomain.Service.Services.Interfaces;
using Sample.EmployeeSubdomain.Service.Settings;
using Sample.EmployeeSubdomain.Service.DatabaseContext;
using Sample.EmployeeSubdomain.Service.Interfaces;
using Sample.EmployeeSubdomain.Service.Entities;
using Sample.EmployeeSubdomain.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Azure;
using Azure.Messaging.ServiceBus;

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
