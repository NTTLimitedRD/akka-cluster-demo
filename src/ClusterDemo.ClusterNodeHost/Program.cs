using Serilog;
using System;
using System.IO;
using System.Threading;

namespace ClusterDemo.ClusterNodeHost
{
    using Actors.Service;

    class Program
    {
        static void Main(string[] commandLineArguments)
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            ConfigureLogging();

            try
            {
                Log.Information("Starting cluster...");

                ClusterApp app = new ClusterApp(
                    actorSystemName: "ClusterDemo",
                    host: "127.0.0.1",
                    port: Int32.Parse(commandLineArguments[0]),
                    seedNodes: new[] { "akka.tcp://ClusterDemo@127.0.0.1:14121" },
                    wampHostUri: new Uri("ws://127.0.0.1:14501"),
                    initialWorkerCount: 5
                );
                app.Start();

                Log.Information("Cluster running (press enter to terminate).");

                Console.ReadLine();

                Log.Information("Stopping cluster...");
                app.Stop();
                Log.Information("Cluster stopped.");
            }
            catch (Exception unexpectedError)
            {
                Log.Error(unexpectedError, "Unexpected error: {ErrorMessage}", unexpectedError.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
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
