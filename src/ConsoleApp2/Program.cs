using Akka.Actor;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Contracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
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
