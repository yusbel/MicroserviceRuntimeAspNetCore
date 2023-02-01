using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sample.EmployeeSubdomain.Employee.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee
{
    public static class ServiceDependency
    {
        public static IServiceCollection AddEmployeeServiceDependency(this IServiceCollection services, IConfiguration configuration) 
        {
            services.Configure<DatabaseSettingOptions>(configuration.GetSection(DatabaseSettingOptions.DatabaseSetting));
            services.Configure<StorageLocationOptions>(configuration.GetSection(StorageLocationOptions.StorageLocation));
            return services;
        }
    }
}
