using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PubSub = Akka.Cluster.Tools.PublishSubscribe;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Common.Messages;
    using Messages;

    public class WorkerEventForwarder
        : ReceiveActorEx, IWithBoundedStash
    {
        public static readonly string ActorName = "worker-event-forwarder";

        readonly IActorRef _workerEvents;
        readonly IActorRef _pubSub;
        bool _isDispatcherLocal;

        public WorkerEventForwarder(IActorRef workerEvents)
        {
            _workerEvents = workerEvents;
            _pubSub = PubSub.DistributedPubSub.Get(Context.System).Mediator;
        }

        public IStash Stash { get; set; }


        void WaitingForDispatcher()
        {
            Log.Info("Worker event forwarder {WorkerEventForwarder} is waiting for the dispatcher to become available.", Self.Path);

            Receive<PubSub.SubscribeAck>(subscribed =>
            {
                Log.Info("WorkerEventForwarder got SubscribeAck for '{Topic}'", subscribed.Subscribe.Topic);
            });
            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                _isDispatcherLocal = dispatcherAvailable.Dispatcher.Path.Address.HasLocalScope;
                if (!_isDispatcherLocal)
                    SubscribeToWorkerEvents();
                else
                    UnsubscribeFromWorkerEvents();

                Become(Ready);
            });

            SubscribeToWorkerEvents();
            Receive<IWorkerEvent>(
                evt => Stash.Stash()
            );
        }

        void Ready()
        {
            Log.Info("Worker event forwarder {WorkerEventForwarder} has found the dispatcher ({LocalDispatcher}) and is ready to forward events.",
                Self.Path,
                _isDispatcherLocal ? "Local" : "Remote"
            );

            Stash.UnstashAll();

            Receive<IWorkerEvent>(workerEvent =>
            {
                Log.Info("WorkerEventForwarder {WorkerEventForwarder} received {EventType} event ({Forward} forward).",
                    Self.Path,
                    workerEvent.GetType().Name,
                    _isDispatcherLocal ? "won't" : "will"
                );

                if (_isDispatcherLocal)
                    return;

                _pubSub.Tell(new PubSub.Publish(
                    topic: "worker",
                    message: workerEvent
                ));
            });

            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                _isDispatcherLocal = dispatcherAvailable.Dispatcher.Path.Address.HasLocalScope;
                if (!_isDispatcherLocal)
                    SubscribeToWorkerEvents();
                else
                    UnsubscribeFromWorkerEvents();
            });
        }

        void SubscribeToWorkerEvents()
        {
            _workerEvents.Tell(
                new Subscribe(Self, eventTypes: new[]
                {
                    typeof(WorkerAvailable),
                    typeof(JobCreated),
                    typeof(JobStarted),
                    typeof(JobCompleted)
                })
            );
        }

        void UnsubscribeFromWorkerEvents()
        {
            _workerEvents.Tell(
                new Unsubscribe(Self)
            );
        }

        protected override void PreStart()
        {
            base.PreStart();

            _pubSub.Tell(
                new PubSub.Subscribe("dispatcher", Self)
            );

            Become(WaitingForDispatcher);
        }
    }
}
