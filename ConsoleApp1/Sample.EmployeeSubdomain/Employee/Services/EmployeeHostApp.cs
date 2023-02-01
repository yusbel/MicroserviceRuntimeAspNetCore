// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Employee.Interfaces;
using Sample.EmployeeSubdomain.Employee.Services;
using System.ComponentModel;

public class EmployeeHostApp : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EmployeeHostApp(IServiceScopeFactory serviceScopeFactory) => 
        (_serviceScopeFactory) = (serviceScopeFactory);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeAppService>();
        return employeeService.ExecuteAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    
}