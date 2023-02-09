using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Service.Services.Interfaces
{
    public interface IEmployeeAppService
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
