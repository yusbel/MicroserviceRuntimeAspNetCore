// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sample.EmployeeSubdomain.Service.Services.Interfaces;
using System.ComponentModel;

public class EmployeeHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IEmployeeAppService _employeeAppService;

    public EmployeeHostedService(IServiceScopeFactory serviceScopeFactory, IEmployeeAppService employeeAppService)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _employeeAppService = employeeAppService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var employeeService = scope.ServiceProvider.GetRequiredService<IEmployeeAppService>();
        return _employeeAppService.ExecuteAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    
}