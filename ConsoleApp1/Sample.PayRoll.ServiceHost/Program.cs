using Microsoft.AspNetCore.Builder;
using Sample.PayRoll;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg;
using Sample.Sdk.Persistance.Context;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Sample.PayRoll.Services.gRPC;
using Sample.Sdk.Msg.Data.Options;
using Sample.PayRoll.Host;
using SampleSdkRuntime;

var serviceArgs = new List<string>(args) { { "PayRollService" }, { "1270015400" } };

Console.WriteLine("Starting pay roll service");
var host = HostService.CreateGenericHost(serviceArgs.ToArray());
await ServiceRuntime.RunAsync(serviceArgs.ToArray(), host).ConfigureAwait(false);
Console.WriteLine("Stopped pay roll service");
Console.ReadLine();

//WebApplicationBuilder appBuilder = WebApplication.CreateBuilder(args);

//appBuilder.Services.AddHostedService<PayRollHostApp>()
//        .AddPayRollServiceDependencies(appBuilder.Configuration)
//        .AddSampleSdk(appBuilder.Configuration)
//        .AddGrpc();

//appBuilder.WebHost.ConfigureKestrel(option =>
//{
//    option.ConfigureHttpsDefaults(httpsOptions =>
//    {
//        httpsOptions.AllowAnyClientCertificate();
//    });
//    option.ListenAnyIP(5243, configure =>
//    {
//        configure.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
//        //configure.UseHttps(@"C:\Users\yusbe\source\repos\LearningDotnet\ConsoleApp1\ConsoleApp1\Cert\localhostcert.pfx", "yusbel");
//        configure.UseHttps();
//    });
//});

//var payRollApp = appBuilder.Build();

//var serviceBusInfoOptions = payRollApp.Services.GetRequiredService<IOptions<List<AzureMessageSettingsOptions>>>().Value;

//payRollApp.MapGrpcService<PayRollService>();
//payRollApp.MapGet("/WebHook", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

//await Task.Delay(1000);
//Console.WriteLine("===========================PayRoll Service==================================");
//await payRollApp.RunAsync();