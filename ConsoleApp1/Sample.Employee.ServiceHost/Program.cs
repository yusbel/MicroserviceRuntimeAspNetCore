// See https://aka.ms/new-console-template for more information
using Sample.EmployeeSubdomain.Host;
using SampleSdkRuntime;

Console.WriteLine($"Processor count ${Environment.ProcessorCount}");

string[] serviceArgs = new string[] { "EmployeeService-0123456789" };

var employeeHost = HostService.Create(serviceArgs);
await ServiceRuntime.RunAsync(serviceArgs, employeeHost.GetHostBuilder()).ConfigureAwait(false);

Console.WriteLine("Service is stop ...");
Console.ReadKey();