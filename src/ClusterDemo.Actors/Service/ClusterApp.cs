using Akka.Actor;
using Akka.Cluster;
using Akka.Cluster.Tools.Singleton;
using Akka.Configuration;
using Akka.Logger.Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    public class ClusterApp
    {
        readonly object _stateLock = new object();
        readonly int _port;
        readonly Address[] _seedNodes;
        ActorSystem _system;

        public ClusterApp(int port, params string[] seedNodes)
        {
            _port = port;

            if (seedNodes.Length == 0)
                throw new ArgumentException("Must specify at least one seed node.", nameof(seedNodes));

            _seedNodes = new Address[seedNodes.Length];
            for (int seedNodeIndex = 0; seedNodeIndex < seedNodes.Length; seedNodeIndex++)
                _seedNodes[seedNodeIndex] = Address.Parse(seedNodes[seedNodeIndex]);
        }

        public void Start()
        {
            lock (_stateLock)
            {
                if (_system != null)
                    throw new ArgumentNullException("Cluster app is already running.");

                _system = ActorSystem.Create(
                    name: "ClusterApp",
                    config: CreateConfig()
                );
                Cluster.Get(_system).JoinSeedNodes(_seedNodes);
            }
        }

        Config CreateConfig()
        {
            return new ConfigBuilder()
                .AddLogger<SerilogLogger>()
                .UseClusterActorRefProvider()
                .UseRemoting("127.0.0.1", _port)
                .SuppressJsonSerializerWarning()
                .Build();
        }
    }
}
