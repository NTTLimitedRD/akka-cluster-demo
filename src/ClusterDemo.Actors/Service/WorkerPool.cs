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

        IActorRef _dispatcher;
        IActorRef _pubSub;

        public WorkerPool(int workerCount)
        {
            _workerCount = workerCount;

            Log.Info("Worker Pool started.");

            // Tell interested parties that a Dispatcher is now available.
            Log.Info("WorkerPool subcribing to dispatcher availability...");
            _pubSub = PubSub.DistributedPubSub.Get(Context.System).Mediator;
            _pubSub.Tell(
                new PubSub.Subscribe("dispatcher", Self)
            );
        }

        void WaitingForDispatcher()
        {
            Log.Info("Worker pool '{WorkerPool}' is waiting for the dispatcher to become available.", Self.Path);

            Receive<PubSub.SubscribeAck>(subscribed =>
            {
                Log.Info("WorkerPool got SubscribeAck for '{Topic}'", subscribed.Subscribe.Topic);
            });
            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                CaptureDispatcher(dispatcherAvailable);

                Become(Ready);
            });
            HandleWorkerTermination();
        }

        void Ready()
        {
            Log.Info("Worker pool '{WorkerPool}' has found the dispatcher and is now creating workers.", Self.Path);

            CreateWorkers();

            Log.Info("Worker pool '{WorkerPool}' created {WorkerCount} workers.",
                Self.Path,
                _workerCount
            );

            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                _dispatcher = dispatcherAvailable.Dispatcher;

                // Notify all workers.
                Context.ActorSelection("*").Tell(dispatcherAvailable);
            });
            HandleWorkerTermination();
        }

        void CaptureDispatcher(DispatcherAvailable dispatcherAvailable)
        {
            _dispatcher = dispatcherAvailable.Dispatcher;

            // Notify all workers.
            Context.ActorSelection("*").Tell(dispatcherAvailable);
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

            Become(WaitingForDispatcher);
        }

        void CreateWorkers()
        {
            for (int workerId = 1; workerId <= _workerCount; workerId++)
            {
                IActorRef worker = Context.ActorOf(
                    Worker.Create(workerId, _dispatcher),
                    name: $"worker-{workerId}"
                );
                Context.Watch(worker);
                _workers.Add(worker);
            }
        }

        public static Props Create(int workerCount)
        {
            return Props.Create<WorkerPool>(workerCount);
        }
    }
}
