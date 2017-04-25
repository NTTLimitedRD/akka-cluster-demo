using Akka.Actor;
using System;
using System.Collections.Generic;

using PubSub = Akka.Cluster.Tools.PublishSubscribe;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Messages;

    public class WorkerPool
        : ReceiveActorEx
    {
        public static readonly string ActorName = "worker-pool";

        readonly int _workerCount;
        readonly List<IActorRef> _workers = new List<IActorRef>();

        IActorRef _workerEvents;
        IActorRef _pubSub;

        public WorkerPool(int workerCount, IActorRef workerEvents)
        {
            _workerCount = workerCount;
            _workerEvents = workerEvents;
            _pubSub = PubSub.DistributedPubSub.Get(Context.System).Mediator;

            Log.Info("Worker pool {WorkerPool} started.", Self);

            CreateWorkers();

            Log.Info("Worker pool {WorkerPool} created {WorkerCount} workers.",
                Self.Path,
                _workerCount
            );

            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                Context.ActorSelection("*")
                    .Tell(dispatcherAvailable);
            });

            HandleWorkerTermination();
        }

        void HandleWorkerTermination()
        {
            Receive<Terminated>(terminated =>
            {
                int workerIndex = _workers.IndexOf(terminated.ActorRef);
                if (workerIndex == -1)
                {
                    Log.Warning("Received unexpected termination notice for '{TerminatedActor}'.", terminated.ActorRef);

                    return;
                }

                Log.Warning("Worker {WorkerId} terminated.",
                    terminated.ActorRef.Path.Name.Replace("worker-", "")
                );

                _workers.RemoveAt(workerIndex);
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                maxNrOfRetries: 3,
                withinTimeRange: TimeSpan.FromSeconds(5),
                decider: Decider.From(Directive.Restart)
            );
        }

        protected override void PreStart()
        {
            base.PreStart();

            _pubSub.Tell(
                new PubSub.Subscribe("dispatcher", Self)
            );
        }

        void CreateWorkers()
        {
            for (int workerId = 1; workerId <= _workerCount; workerId++)
            {
                IActorRef worker = Context.ActorOf(
                    Worker.Create(workerId, _workerEvents),
                    name: $"worker-{workerId}"
                );
                Context.Watch(worker);
                _workers.Add(worker);
            }
        }

        public static Props Create(int workerCount, IActorRef workerEvents)
        {
            return Props.Create<WorkerPool>(workerCount, workerEvents);
        }
    }
}
