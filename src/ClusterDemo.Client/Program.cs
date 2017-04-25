using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.IO;
using System.Threading;

namespace ClusterDemo.Client
{
    using Actors.Client;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                const int webPort = 14600;
                const int akkaPort = webPort + 10;

                SynchronizationContext.SetSynchronizationContext(
                    new SynchronizationContext()
                );

                ConfigureLogging();

                Console.WriteLine($"Starting cluster client on port {akkaPort}...");
                ClientApp clientApp = new ClientApp(
                    actorSystemName: "ClusterDemoClient",
                    host: "127.0.0.1",
                    port: akkaPort,
                    clusterContactNodes: new[] { "akka.tcp://ClusterDemo@127.0.0.1:14121" }
                );
                clientApp.Start();

                using (IWebHost webHost = CreateWebHost(clientApp, webPort))
                {
                    Console.WriteLine($"Starting web host on port {webPort}...");
                    webHost.Start();
                    Console.WriteLine($"Web host listening on http://127.0.0.1:{webPort}...");

                    Console.ReadLine();
                }

                clientApp.Stop();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static IWebHost CreateWebHost(ClientApp clientApp, int port)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:{port}")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(
                    services => services.AddSingleton(clientApp)
                )
                .UseStartup<Startup>()
                .Build();
        }

        static void ConfigureLogging()
        {
            string[] commandLine = Environment.GetCommandLineArgs();
            commandLine[0] = Path.GetFileNameWithoutExtension(commandLine[0]);

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty(
                    name: "Program",
                    value: String.Join(" ", commandLine)
                )
                .WriteTo.Seq("http://localhost:5341/",
                    apiKey: "MHXFLikIgkbTIsfJuTYP",
                    period: TimeSpan.FromSeconds(1)
                )
                .WriteTo.ColoredConsole(
                    outputTemplate: "[{Level}] {Message}{NewLine}{Exception}"
                )
                .CreateLogger();
        }
    }
}
