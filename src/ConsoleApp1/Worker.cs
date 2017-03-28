using Akka.Actor;
using Contracts;
using System;
using System.Threading;

namespace ConsoleApp1
{
    public class Worker : UntypedActor
    {
        readonly int _workerId;

        public Worker(int workerId)
        {
            _workerId = workerId;
        }

        protected override void OnReceive(object message)
        {
            var job = (ProvisionJob)message;
            Console.WriteLine($" Worker {_workerId} Path:{Self.Path.Name}. JobId: {job.JobId}, UserId: {job.UserId} and username: {job.UserName}.");
            Thread.Sleep(1000);


            Sender.Tell(new ProvisionJobCompleted
            {
                JobId = job.JobId
            });
        }
    }
}
