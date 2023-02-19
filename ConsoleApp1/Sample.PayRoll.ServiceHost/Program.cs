using Microsoft.AspNetCore.Builder;
using Sample.PayRoll;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Msg;
using Sample.Sdk.Persistance.Context;
using Microsoft.Extensions.DependencyInjection;
using Sample.Sdk;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Sample.PayRoll.Services.gRPC;

///$Env: AZURE_CLIENT_ID = "51df4bce-6532-4345-9be7-5be7af315003"
/// $Env:AZURE_CLIENT_SECRET="tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2"
/// $Env:AZURE_TENANT_ID="c8656f45-daf5-42c1-9b29-ac27d3e63bf3"

Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", "51df4bce-6532-4345-9be7-5be7af315003");
Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", "tdm8Q~Cw_e7cLFadttN7Zebacx_kC5Y-0xaWZdv2");
Environment.SetEnvironmentVariable("AZURE_TENANT_ID", "c8656f45-daf5-42c1-9b29-ac27d3e63bf3");

WebApplicationBuilder appBuilder = WebApplication.CreateBuilder(args);

appBuilder.Services.AddHostedService<PayRollHostApp>()
        .AddPayRollServiceDependencies(appBuilder.Configuration)
        .AddSampleSdk(appBuilder.Configuration, "PayRoll:AzureServiceBusInfo:Configuration")
        .AddGrpc();

appBuilder.WebHost.ConfigureKestrel(option =>
{
    option.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.AllowAnyClientCertificate();
    });
    option.ListenAnyIP(5243, configure =>
    {
        configure.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
        //configure.UseHttps(@"C:\Users\yusbe\source\repos\LearningDotnet\ConsoleApp1\ConsoleApp1\Cert\localhostcert.pfx", "yusbel");
        configure.UseHttps();
    });
});

var payRollApp = appBuilder.Build();

var serviceBusInfoOptions = payRollApp.Services.GetRequiredService<IOptions<List<ServiceBusInfoOptions>>>().Value;

payRollApp.MapGrpcService<PayRollService>();
payRollApp.MapGet("/WebHook", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

await Task.Delay(1000);
Console.WriteLine("===========================PayRoll Service==================================");
await payRollApp.RunAsync();