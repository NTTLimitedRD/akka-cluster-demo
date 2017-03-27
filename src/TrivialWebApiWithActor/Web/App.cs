using Akka.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TrivialWebApiWithActor.Web
{
    public static class App
    {
        public static IWebHost Create(IActorRef greeter)
        {
            return new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(greeter);
                })
                .UseStartup<Startup>()
                .UseUrls("http://+:19123/")
                .UseKestrel()
                .Build();
        }

        class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddMvc();
            }

            public void Configure(IApplicationBuilder app, ILoggerFactory loggers)
            {
                loggers.AddConsole(LogLevel.Warning);
                
                app.UseMvcWithDefaultRoute();
            }
        }
    }
}