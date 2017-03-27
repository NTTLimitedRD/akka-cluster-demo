using Akka.Actor;
using Akka.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading;

namespace TrivialWebApiWithActor
{
    static class Program
    {
        static readonly Config DefaultConfig = ConfigurationFactory.ParseString(@"
            akka {
                suppress-json-serializer-warning = on
            }
        ");

        static void Main()
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            using (var system  = ActorSystem.Create("Demo", DefaultConfig))
            {
                IActorRef greeter = system.ActorOf(Greeter.Create);
                greeter.Tell(new PrintGreet
                {
                    Name = "World"
                });

                using (IWebHost webHost = Web.App.Create(greeter))
                {
                    webHost.Start();

                    Console.WriteLine("Press enter to terminate.");
                    Console.ReadLine();

                    Console.WriteLine("Shutting down...");
                }

                system.Terminate().Wait();
                Console.WriteLine("Shutdown complete.");
            }
        }
    }

    class Greeter
        : ReceiveActor
    {
        public static readonly Props Create = Props.Create<Greeter>();

        public Greeter()
        {
            Receive<PrintGreet>(greet =>
            {
                Console.WriteLine($"Hello, {greet.Name}!");
            });

            Receive<GreetMe>(greet =>
            {
                Console.WriteLine($"Greeting '{greet.Name}'...");

                // Reply with greeting.
                Sender.Tell($"Hello, {greet.Name}!");
            });
        }
    }

    public class PrintGreet
    {
        public string Name { get; set; }
    }

    public class GreetMe
    {
        public string Name { get; set; }
    }
}
