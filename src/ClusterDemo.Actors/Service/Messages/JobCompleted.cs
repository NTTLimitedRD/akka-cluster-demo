using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ClusterDemo.Actors.Service.Messages
{
    public class JobCompleted
        : IWorkerEvent
    {
        public JobCompleted(int id, IActorRef worker, TimeSpan jobTime, params string[] messages)
            : this(id, worker, jobTime, (IEnumerable<string>)messages)
        {
        }

        public JobCompleted(int id, IActorRef worker, TimeSpan jobExecutionTime, IEnumerable<string> messages)
        {
            Id = id;
            Worker = worker;
            messages = ImmutableList.CreateRange(messages);
        }

        public int Id { get; }
        public IActorRef Worker { get; }
        public TimeSpan JobExecutionTime { get; }
        public ImmutableList<string> Messages;
    }
}
