using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Logger.Serilog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    public class ClusterApp
    {
        readonly object _stateLock = new object();
        readonly Address[]  _seedNodes;
        Uri _wampHostUri;
        ActorSystem _system;

        public ClusterApp(string actorSystemName, string host, int port, string[] seedNodes, Uri wampHostUri)
        {
            LocalNodeAddress = new Address("akka.tcp", actorSystemName, host, port);

            if (seedNodes.Length == 0)
                throw new ArgumentException("Must specify at least one seed node.", nameof(seedNodes));

            _seedNodes = new Address[seedNodes.Length];
            for (int seedNodeIndex = 0; seedNodeIndex < seedNodes.Length; seedNodeIndex++)
                _seedNodes[seedNodeIndex] = Address.Parse(seedNodes[seedNodeIndex]);

            _wampHostUri = wampHostUri;
        }

        Address LocalNodeAddress { get; }

        public void Start()
        {
            lock (_stateLock)
            {
                if (_system != null)
                    throw new ArgumentNullException("Cluster app is already running.");

                Log.Information("Starting actor system {LocalNodeAddress}...", LocalNodeAddress);
                _system = ActorSystem.Create(
                    name: LocalNodeAddress.System,
                    config: CreateConfig()
                );
                
                // Node monitor (one per node).
                IActorRef nodeMonitor = _system.ActorOf(
                    NodeMonitor.Create(_wampHostUri),
                    name: NodeMonitor.ActorName
                );

                // Worker event bus (one per node).
                IActorRef workerEvents = _system.ActorOf(
                    Props.Create<WorkerEvents>(),
                    name: WorkerEvents.ActorName
                );

                IActorRef statsCollector = _system.ActorOf(
                    StatsCollector.Create(nodeMonitor, workerEvents, LocalNodeAddress)
                );

                // Dispatcher (cluster-wide singleton).
                // TODO: Work out why there are dead-lettered messages relating to distributed PubSub (they don't include the address part of the actor path, which may be a configuration issue).
                _system.ActorOf(
                    ClusterSingletonManager.Props(
                        singletonProps: Props.Create<Dispatcher>(),
                        terminationMessage: PoisonPill.Instance, // TODO: Use a more-specific message
                        settings: ClusterSingletonManagerSettings.Create(_system)
                    ),
                    name: Dispatcher.ActorName
                );
            }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (_system == null)
                    throw new ArgumentNullException("Cluster app is not running.");

                Log.Information("Leaving cluster...");
                Cluster.Get(_system).LeaveAsync(CancellationToken.None)
                    .ContinueWith(_ =>
                    {
                        Log.Information("Terminating actor system...");

                        _system.Terminate();
                        _system = null;
                    })
                    .Wait();
            }
        }

        Config CreateConfig()
        {
            return new ConfigBuilder()
                .AddLogger<SerilogLogger>()
                .SetLogLevel(Akka.Event.LogLevel.InfoLevel)
                .UseCluster(_seedNodes)
                .UseRemoting(LocalNodeAddress.Host, LocalNodeAddress.Port.Value)
                .SuppressJsonSerializerWarning()
                .Build();
        }
    }
}
