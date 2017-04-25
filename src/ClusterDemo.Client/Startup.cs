using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ClusterDemo.Client
{
	public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            loggerFactory.AddSerilog();

			app.UseDeveloperExceptionPage();

			app.UseStaticFiles();
			app.UseDefaultFiles(new DefaultFilesOptions
			{
				DefaultFileNames = { "index.html" }
			});

            app.UseMvcWithDefaultRoute();

            // Ensure any buffered events are logged at shutdown
            appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);
        }
    }
}
