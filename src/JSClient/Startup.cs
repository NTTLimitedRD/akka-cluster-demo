using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JSClient
{
	public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

			app.UseDeveloperExceptionPage();

			app.UseStaticFiles();
			app.UseDefaultFiles();
        }
    }
}
