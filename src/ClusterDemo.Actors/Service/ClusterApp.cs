using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.Logger.Serilog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    public class ClusterApp
    {
        readonly object _stateLock = new object();

        ActorSystem _system;
        IActorRef _clusterNodeManager;

        public ClusterApp(string actorSystemName, string host, int port, IEnumerable<string> seedNodes, Uri wampHostUri, int initialWorkerCount)
        {
            LocalNodeAddress = new Address("akka.tcp", actorSystemName, host, port);

            SeedNodes = seedNodes.Select(Address.Parse).ToArray();
            if (SeedNodes.Count == 0)
                throw new ArgumentException("Must specify at least one seed node.", nameof(seedNodes));

            WampHostUri = wampHostUri;
            InitialWorkerCount = initialWorkerCount;
        }

        public Address LocalNodeAddress { get; }

        public IReadOnlyList<Address> SeedNodes { get; }

        public Uri WampHostUri { get; }

        public int InitialWorkerCount { get; }

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

                _clusterNodeManager = _system.ActorOf(ClusterNodeManager.Create(
                    localNodeAddress: LocalNodeAddress,
                    wampHostUri: WampHostUri,
                    initialWorkerCount: InitialWorkerCount
                ));
            }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (_system == null)
                    throw new ArgumentNullException("Cluster app is not running.");

                try
                {
                    StopAsync().Wait();
                }
                catch (AggregateException aggregateException) // Unwrap, if appropriate.
                {
                    AggregateException flattenedAggregate = aggregateException.Flatten();
                    if (flattenedAggregate.InnerExceptions.Count > 1)
                        throw; // Genuine aggregate.

                    ExceptionDispatchInfo
                        .Capture(flattenedAggregate.InnerExceptions[0])
                        .Throw();
                }
            }
        }

        async Task StopAsync()
        {
            try
            {
                if (_clusterNodeManager != null)
                {
                    Log.Information("Shutting down cluster node manager...");

                    await _clusterNodeManager.GracefulStop(
                        timeout: TimeSpan.FromSeconds(10)
                    );
                }

                Log.Information("Leaving cluster...");

                CancellationTokenSource cancellation = new CancellationTokenSource();
                cancellation.CancelAfter(
                    delay: TimeSpan.FromSeconds(10)
                );
                await Cluster.Get(_system).LeaveAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Timed out after waiting 10 seconds to leave the cluster; continuing with shutdown.");
            }

            Log.Information("Shutting down local actor system...");

            await _system.Terminate();
        }

        Config CreateConfig()
        {
            return new ConfigBuilder()
                .AddLogger<SerilogLogger>()
                .SetLogLevel(Akka.Event.LogLevel.InfoLevel)
                .UseCluster(
                    seedNodes: SeedNodes,
                    minNumberOfMembers: 1 // For demo purposes, we have a small cluster to play with and don't want to stuff around with lighthouses.
                )
                .UseRemoting(LocalNodeAddress.Host, LocalNodeAddress.Port.Value)
                .UseHyperionSerializer()
                .Build();
        }
    }
}
