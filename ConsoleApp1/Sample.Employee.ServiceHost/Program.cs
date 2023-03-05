// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sample.EmployeeSubdomain.Host;
using SampleSdkRuntime;

Console.WriteLine($"Processor count ${Environment.ProcessorCount}");

var serviceArgs = new List<string>();
serviceArgs.AddRange(args);
var employeeHost = HostService.Create(serviceArgs.ToArray());
employeeHost.GetHostBuilder().ConfigureServices((host, services) => 
{
    serviceArgs.Add(host.Configuration.GetValue<string>("Service:ServiceInstance:ServiceIdentifier"));
    serviceArgs.Add(host.Configuration.GetValue<string>("Service:Name"));
});
await ServiceRuntime.RunAsync(serviceArgs.ToArray(), employeeHost.GetHostBuilder()).ConfigureAwait(false);

Console.WriteLine("Service is stop ...");
Console.ReadKey();