using System;
using System.Collections.Immutable;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Messages;

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
