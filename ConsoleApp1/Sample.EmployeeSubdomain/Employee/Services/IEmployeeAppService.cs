using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample.EmployeeSubdomain.Employee.Services
{
    public interface IEmployeeAppService
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
