using SampleSdkRuntime.Http.Middleware;

namespace SampleSdkRuntime.Host
{
    public class RuntimeStartUp
    {
        public void ConfigureServices(IServiceCollection services) 
        {
        }

        public void Configure(IApplicationBuilder appBuilder, IWebHostEnvironment environment) 
        {
            appBuilder.UseMiddleware<CryptoMiddleware>();

            appBuilder.Run(async (ctx) => 
            {
                await ctx.Response.WriteAsync("Hello World").ConfigureAwait(false);
            });
        }
    }
}