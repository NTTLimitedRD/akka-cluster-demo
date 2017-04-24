using System;
using System.Collections.Immutable;

namespace ClusterDemo.Actors.Service.Messages
{
    using Common;

    public class WorkerEvents
        : EventBusActor<IWorkerEvent>
    {
        public WorkerEvents()
        {
        }

        protected override ImmutableList<Type> AllEventTypes { get; } = ImmutableList.Create(
            typeof(WorkerAvailable),
            typeof(JobCompleted)
        );
    }
}
