using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Sample.EmployeeSubdomain.Services.Interfaces;
using Sample.EmployeeSubdomain.Services;
using Sample.EmployeeSubdomain.Entities;
using Sample.EmployeeSubdomain.Interfaces;
using Sample.EmployeeSubdomain.Settings;
using Sample.EmployeeSubdomain.DatabaseContext;
using Sample.Sdk.Interface.Msg;
using Sample.Sdk.Interface.Database;
using Sample.Sdk.Data.Options;
using Sample.Sdk.Core.DatabaseContext;

namespace Sample.EmployeeSubdomain
{
    public static class EmployeeServiceDependecyExtensions
    {
        public static IServiceCollection AddEmployeeServiceDependencies(this IServiceCollection services, IConfiguration configuration) 
        {
            services.Configure<List<ExternalValidEndpointOptions>>(configuration.GetSection(ExternalValidEndpointOptions.SERVICE_SECURITY_VALD_ENDPOINTS_ID));
            services.AddTransient<IComputeExternalMessage, ComputeExternalMessage>();
            services.AddTransient<IEmployee, Employee>();
            services.AddTransient<IEntityContext<EmployeeContext, EmployeeEntity>, EntityContext<EmployeeContext, EmployeeEntity>>();
            services.AddDbContext<EmployeeContext>(options =>
            {
                options.UseSqlServer(sqlDbOptions => 
                {
                });
                options.EnableDetailedErrors(true);
            });
            services.AddHostedService<EmployeeGenerator>();
            services.AddSingleton<IMessageSenderService, MessageSenderService>();
            services.Configure<StorageLocationOptions>(configuration.GetSection(StorageLocationOptions.StorageLocation));
            
            return services;
        }
    }
}
