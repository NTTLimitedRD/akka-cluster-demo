using Serilog;
using System;
using System.Threading;

namespace ClusterDemo.ClusterNodeHost
{
    using Actors.Service;

    class Program
    {
        static void Main(string[] args)
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            ConfigureLogging();

            try
            {
                ClusterApp app = new ClusterApp(
                    port: Int32.Parse(args[0]),
                    seedNodes: new[]
                    {
                        "akka.tcp://ClusterApp@127.0.0.1:14121",
                        "akka.tcp://ClusterApp@127.0.0.1:14122"
                    }
                );
                app.Start();

                Console.ReadLine();
            }
            catch (Exception unexpectedError)
            {
                Log.Error(unexpectedError, "Unexpected error: {ErrorMessage}", unexpectedError.Message);
            }
        }

        static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.ColoredConsole(
                    outputTemplate: "[{Level}] {Message}{NewLine}{Exception}"
                )
                .CreateLogger();
        }
    }
}
