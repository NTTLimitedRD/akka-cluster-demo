using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Configuration;
using Akka.Logger.Serilog;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClusterDemo.Actors.Client
{
    using Common.Messages;

    public class ClientApp
    {
        readonly object _stateLock = new object();
        readonly IReadOnlyList<Address> _clusterContactNodes;

        ActorSystem _system;
        IActorRef _jobClient;

        public ClientApp(string actorSystemName, string host, int port, string[] clusterContactNodes)
        {
            LocalNodeAddress = new Address("akka.tcp", actorSystemName, host, port);

            if (clusterContactNodes.Length == 0)
                throw new ArgumentException("Must specify at least one cluster contact node.", nameof(clusterContactNodes));

            _clusterContactNodes = new List<Address>(
                clusterContactNodes.Select(
                    nodeAddress => Address.Parse(nodeAddress)
                )
            );
        }

        public Address LocalNodeAddress { get; }

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

                _system.Settings.InjectTopLevelFallback(ClusterClientReceptionist.DefaultConfig());
                IActorRef clusterClient = _system.ActorOf(
                    ClusterClient.Props(
                        ClusterClientSettings.Create(_system)
                    ),
                    name: "cluster-client"
                );

                _jobClient = _system.ActorOf(
                    Props.Create(
                        () => new JobClient(clusterClient)
                    ),
                    name: "job-client"
                );
            }
        }

        public void Stop()
        {
            lock (_stateLock)
            {
                if (_system == null)
                    throw new ArgumentNullException("Cluster app is not running.");

                Log.Information("Terminating actor system...");

                _system.Terminate().Wait();
                _system = null;

                Log.Information("Actor system terminated.");
            }
        }

        public void SubmitJob(string name)
        {
            _jobClient.Tell(
                new CreateJob(name)
            );
        }

        Config CreateConfig()
        {
            return new ConfigBuilder()
                .AddLogger<SerilogLogger>()
                .SetLogLevel(Akka.Event.LogLevel.InfoLevel)
                .UseRemoting(LocalNodeAddress.Host, LocalNodeAddress.Port.Value)
                .UseRemoteActorRefProvider()
                .UseHyperionSerializer()
                .UseClusterClient(_clusterContactNodes)
                .Build();
        }
    }
}
