using Akka.Actor;
using Akka.Configuration.Hocon;
using Contracts;
using System;
using System.Configuration;

namespace SimpleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var section = (AkkaConfigurationSection)ConfigurationManager.GetSection("akka");
            var system = ActorSystem.Create("ClusterSystem", section.AkkaConfig);

            IActorRef client = system.ActorOf<ClientActor>();

            for (int i = 0; i < 20; i++)
                client.Tell(new ProvisionJob
                {
                    JobId = $"Job {i}",
                    UserId = Guid.NewGuid(),
                    UserName = $"User {i}"
                });

            Console.Read();
        }
    }
}
