using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;

namespace WampClusterMonitor
{
	public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

			app.UseDeveloperExceptionPage();

			app.UseStaticFiles();
			app.UseDefaultFiles(new DefaultFilesOptions
			{
				DefaultFileNames = { "index.html" }
			});
        }
    }
}
