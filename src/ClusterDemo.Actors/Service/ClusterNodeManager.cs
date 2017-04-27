using Akka.Actor;
using Akka.Cluster.Tools.Singleton;
using System;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using System.Net.Sockets;

    // TODO: Supervision.

    public class ClusterNodeManager
        : ReceiveActorEx
    {
        readonly Address _localNodeAddress;
        readonly Uri _wampHostUri;
        readonly int _initialWorkerCount;

        IActorRef _nodeMonitor;
        IActorRef _workerEvents;
        IActorRef _workerEventForwarder;
        IActorRef _dispatcher;
        IActorRef _workerPool;
        IActorRef _statsCollector;

        public ClusterNodeManager(Address localNodeAddress, Uri wampHostUri, int initialWorkerCount)
        {
            _localNodeAddress = localNodeAddress;
            _wampHostUri = wampHostUri;
            _initialWorkerCount = initialWorkerCount;
        }

        protected override void PreStart()
        {
            base.PreStart();

            // Node monitor (one per node).
            _nodeMonitor = Context.ActorOf(
                NodeMonitor.Create(_wampHostUri),
                name: NodeMonitor.ActorName
            );

            // Worker event bus (one per node).
            _workerEvents = Context.ActorOf(
                Props.Create<WorkerEvents>(),
                name: WorkerEvents.ActorName
            );

            // Worker event-forwarder.
            _workerEventForwarder = Context.ActorOf(
                Props.Create(
                    () => new WorkerEventForwarder(_workerEvents)
                ),
                name: WorkerEventForwarder.ActorName
            );

            // Dispatcher (cluster-wide singleton).
            _dispatcher = Context.ActorOf(
                ClusterSingletonManager.Props(
                    singletonProps: Dispatcher.Create(_workerEvents),
                    terminationMessage: PoisonPill.Instance, // TODO: Use a more-specific message
                    settings: ClusterSingletonManagerSettings.Create(Context.System)
                ),
                name: Dispatcher.ActorName
            );

            // Worker pool (one per node).
            _workerPool = Context.ActorOf(
                WorkerPool.Create(_initialWorkerCount, _workerEvents),
                name: WorkerPool.ActorName
            );

            // Node statistics collector (one per node).
            _statsCollector = Context.ActorOf(
                StatsCollector.Create(_nodeMonitor, _workerEvents, _localNodeAddress)
            );
        }

        public static Props Create(Address localNodeAddress, Uri wampHostUri, int initialWorkerCount)
        {
            return Props.Create<ClusterNodeManager>(localNodeAddress, wampHostUri, initialWorkerCount);
        }
    }
}
