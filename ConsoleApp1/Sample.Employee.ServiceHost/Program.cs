// See https://aka.ms/new-console-template for more information
using Sample.EmployeeSubdomain.Host;
using SampleSdkRuntime;

Console.WriteLine($"Processor count ${Environment.ProcessorCount}");

var serviceArgs = new List<string>(args) { { "EmployeeService" }, { "1270015500" } };

var employeeHost = HostService.Create(serviceArgs.ToArray());

await ServiceRuntime.RunAsync(serviceArgs.ToArray(), employeeHost.GetHostBuilder()).ConfigureAwait(false);

Console.WriteLine("Service is stop ...");
Console.ReadKey();