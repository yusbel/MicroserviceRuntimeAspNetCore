using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sample.PayRoll;
using Sample.PayRoll.DatabaseContext;
using Sample.PayRoll.Entities;
using Sample.PayRoll.Interfaces;
using Sample.PayRoll.Services.gRPC;
using Sample.Sdk;
using Sample.Sdk.Msg;
using Sample.Sdk.Msg.Data;
using Sample.Sdk.Msg.Interfaces;
using Sample.Sdk.Persistance.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class PayRollStarterHost
    {
        public static void ConfigurePayRollService(string[] args, List<Task> hosts)
        {
            //Console.WriteLine("Payroll start");
            //WebApplicationBuilder appBuilder = WebApplication.CreateBuilder(args);

            //appBuilder.Services.AddHostedService<PayRollHostApp>()
            //        .AddTransient<IPayRoll, PayRoll>()
            //        .AddTransient<IEntityContext<PayRollContext, PayRollEntity>, EntityContext<PayRollContext, PayRollEntity>>()
            //        .AddTransient<IMessageBusSender, ServiceBusMessageSender>()
            //        .AddDbContext<PayRollContext>(options =>
            //        {
            //            options.EnableDetailedErrors(true);
            //        })
            //        .AddSampleSdk()
            //        .AddGrpc();

            //appBuilder.Services.Configure<ServiceBusInfoOptions>(appBuilder.Configuration.GetSection("PayRollService:MsgSdTransactionInfo"));

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

            //payRollApp.MapGrpcService<PayRollService>();
            //payRollApp.MapGet("/WebHook", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

            //hosts.Add(payRollApp.RunAsync());
            
        }
    }
}
