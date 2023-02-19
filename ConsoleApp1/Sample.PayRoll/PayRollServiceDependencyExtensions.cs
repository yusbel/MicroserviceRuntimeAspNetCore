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

namespace Sample.PayRoll
{
    public static class PayRollServiceDependencyExtensions
    {
        public static IServiceCollection AddPayRollServiceDependencies(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IPayRoll, PayRoll>();
            services.AddTransient<IEntityContext<PayRollContext, PayRollEntity>, EntityContext<PayRollContext, PayRollEntity>>();
            services.AddTransient<IMessageBusSender, ServiceBusMessageSender>();
            services.AddSingleton<IMessageBusSender, ServiceBusMessageSender>();
            services.AddSingleton<IMessageBusReceiver<EmployeeAdded>, ServiceBusMessageReceiver<EmployeeAdded>>();
            services.AddHostedService<EmployeeAddedHostedService>();
            services.AddTransient<IEmployeeAddedService, EmployeeAddedService>();
            
            services.AddHttpClient();//TODO:setup polly

            services.AddDbContext<PayRollContext>(options =>
            {
                options.EnableDetailedErrors(true);
            });
            services.AddAzureClients(azureClientFactoryBuilder => 
            {
                
                //EmployeeAdded event
                azureClientFactoryBuilder.AddServiceBusClient(configuration.GetValue<string>("PayRoll:AzureServiceBusInfo:DefaultConnStr"))
                .ConfigureOptions(options => 
                {
                    options.Identifier = "EmployeeAddedService";
                    options.RetryOptions = new ServiceBusRetryOptions() 
                    {
                        Delay = TimeSpan.FromSeconds(configuration.GetValue<int>("PayRoll:Service:Default:RetryOptions:DelayInSeconds")),
                        MaxDelay = TimeSpan.FromSeconds(configuration.GetValue<int>("PayRoll:Service:Default:RetryOptions:MaxDelayInSeconds")),
                        MaxRetries = configuration.GetValue<int>("PayRoll:Service:Default:RetryOptions:MaxRetries"),
                        Mode = configuration.GetValue<string>("PayRoll:Service:Default:RetryOptions:Mode") == "Fixed" ? ServiceBusRetryMode.Fixed : ServiceBusRetryMode.Exponential
                    };
                });
                //PayRollAddedEvent
                azureClientFactoryBuilder.AddServiceBusClient(configuration.GetValue<string>("PayRoll:AzureServiceBusInfo:DefaultConnStr"))
                .ConfigureOptions(options => 
                {
                    options.Identifier = "PayRollAddedService";
                });
            });
            return services;
        }
    }
}
