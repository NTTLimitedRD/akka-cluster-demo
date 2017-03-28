using Akka.Actor;
using Contracts;
using System;

namespace SimpleClient
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
