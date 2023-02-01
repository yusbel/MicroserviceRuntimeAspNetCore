
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.Sdk
{
    public static class RegisterDependency
    {
        public static IServiceCollection AddSampleSdk(this IServiceCollection services)
        {
            //services.AddScoped(typeof(IEntityContext<,>), typeof(IEntityContext<,>)); 
            return services;
        } 
    }
}
