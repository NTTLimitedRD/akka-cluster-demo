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
        readonly int _port;
        readonly Address[] _seedNodes;
        Uri _wampHostUri;
        ActorSystem _system;

        public ClusterApp(int port, string[] seedNodes, Uri wampHostUri)
        {
            _port = port;

            if (seedNodes.Length == 0)
                throw new ArgumentException("Must specify at least one seed node.", nameof(seedNodes));

            _seedNodes = new Address[seedNodes.Length];
            for (int seedNodeIndex = 0; seedNodeIndex < seedNodes.Length; seedNodeIndex++)
                _seedNodes[seedNodeIndex] = Address.Parse(seedNodes[seedNodeIndex]);

            _wampHostUri = wampHostUri;
        }

        public void Start()
        {
            lock (_stateLock)
            {
                if (_system != null)
                    throw new ArgumentNullException("Cluster app is already running.");

                Log.Information("Starting actor system...");
                _system = ActorSystem.Create(
                    name: "ClusterApp",
                    config: CreateConfig()
                );

                Log.Information("Joining cluster...");
                Cluster.Get(_system).JoinSeedNodes(_seedNodes);

                // Node monitor (noe per node).
                IActorRef nodeMonitor = _system.ActorOf(
                    Props.Create(() => new NodeMonitor(_wampHostUri))
                        .WithSupervisorStrategy(SupervisorStrategy.DefaultStrategy)
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
                .UseClusterActorRefProvider()
                .UseRemoting("127.0.0.1", _port)
                .SuppressJsonSerializerWarning()
                .Build();
        }
    }
}
