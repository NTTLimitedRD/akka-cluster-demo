using Serilog;
using System;
using System.Threading;

namespace ClusterDemo.ClusterNodeHost
{
    using Actors.Service;
    using WampSharp.V2;

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
                    },
                    wampHostUri: new Uri("ws://127.0.0.1:14501")
                );
                app.Start();

                Console.ReadLine();

                app.Stop();
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
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole(
                    outputTemplate: "[{Level}] {Message}{NewLine}{Exception}"
                )
                .CreateLogger();
        }
    }
}
