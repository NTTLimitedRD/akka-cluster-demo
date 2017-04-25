using System;
using System.Collections.Immutable;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Messages;

    // AF: We might need a *selective* bridge between this and distributed pub-sub.
    // The stats monitor runs locally, and wants to receive every notification, but maybe the dispatcher doesn't?
    // Otherwise, how does the stats monitor find out about jobs?

    public class WorkerEvents
        : EventBusActor<IWorkerEvent>
    {
        public static readonly string ActorName = "worker-events";

        public WorkerEvents()
        {
        }

        protected override ImmutableList<Type> AllEventTypes { get; } = ImmutableList.Create(
            typeof(WorkerAvailable),
            typeof(JobCompleted)
        );
    }
}
