using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ClusterDemo.UI
{
	public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IApplicationLifetime appLifetime)
        {
            loggerFactory.AddSerilog();

			app.UseDeveloperExceptionPage();

			app.UseStaticFiles();
			app.UseDefaultFiles(new DefaultFilesOptions
			{
				DefaultFileNames = { "index.html" }
			});

            // Ensure any buffered events are logged at shutdown
            appLifetime.ApplicationStopped.Register(Log.CloseAndFlush);
        }
    }
}
