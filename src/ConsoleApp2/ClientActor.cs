using Akka.Actor;
using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    public class ClientActor : ReceiveActor
    {
        public ClientActor()
        {
            Receive<ProvisionJob>(job =>
            {
                Context.ActorSelection("akka.tcp://ClusterSystem@localhost:3551/user/receiver").Tell(job);
                Console.WriteLine(job.JobId + " received");
            });

            Receive<ProvisionJobCompleted>(job =>
            {
                Console.WriteLine(job.JobId + " completed");
            });
        }
    }
}
