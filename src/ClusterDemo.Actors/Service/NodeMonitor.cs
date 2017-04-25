using Akka.Actor;
using Akka.Cluster;
using System;
using WampSharp.V2;
using WampSharp.V2.Client;
using WampSharp.V2.Core.Contracts;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Messages;
    using System.Threading.Tasks;

    public class NodeMonitor
        : ReceiveActorEx
    {
        public static readonly string ActorName = "node-monitor";

        readonly Uri    _wampHostUri;
        IWampChannel    _wampChannel;
        IWampTopicProxy _stateTopic;
        IWampTopicProxy _statsTopic;

        public NodeMonitor(Uri wampHostUri)
        {
            _wampHostUri = wampHostUri;
        }

        void Connected()
        {
            Log.Info("Node monitor '{NodeMonitor}' has established a connection to WAMP server '{WampServerUri}'.",
                Self.Path,
                _wampHostUri.AbsoluteUri
            );

            ReceiveAsync<ClusterEvent.IMemberEvent>(async memberEvent =>
            {
                await PublishStatus(
                    memberEvent.Member.Address.ToString(),
                    memberEvent.Member.Status.ToString()
                );
            });
            ReceiveAsync<ClusterEvent.CurrentClusterState>(async clusterState =>
            {
                foreach (Member member in clusterState.Members)
                {
                    await PublishStatus(
                        member.Address.ToString(),
                        member.Status.ToString()
                    );
                }
            });
            ReceiveAsync<NodeStats>(async nodeStats =>
            {
                await _statsTopic.Publish(new PublishOptions(),
                    new object[] { nodeStats }
                );
            });

            Receive<ConnectionState>(connectionState =>
            {
                if (connectionState == ConnectionState.Disconnected)
                {
                    _stateTopic = null;
                    _statsTopic = null;

                    Become(Disconnected);
                }
                else
                    Unhandled(connectionState);
            });
        }

        void Disconnected()
        {
            Log.Warning("The connection from node monitor '{NodeMonitor}' to WAMP server '{WampServerUri}' has been broken; node statistics messages will be ignored until it is reconnected.",
                Self.Path,
                _wampHostUri.AbsoluteUri
            );

            Receive<ConnectionState>(connectionState =>
            {
                if (connectionState == ConnectionState.Connected)
                {
                    _stateTopic = _wampChannel.RealmProxy.TopicContainer.GetTopicByUri("cluster.node.state");
                    _statsTopic = _wampChannel.RealmProxy.TopicContainer.GetTopicByUri("cluster.node.statistics");

                    Become(Connected);
                }
                else
                    Unhandled(connectionState);
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            Log.Info("Node monitor '{NodeMonitor}' connecting to WAMP host '{WampHostUri}'...",
                Self.Path,
                _wampHostUri.AbsoluteUri
            );

            _wampChannel = new DefaultWampChannelFactory().CreateJsonChannel(
                address: _wampHostUri.AbsoluteUri,
                realm: "ClusterDemo"
            );

            Cluster cluster = Cluster.Get(Context.System);
            cluster.Subscribe(Self,
                typeof(ClusterEvent.MemberJoined),
                typeof(ClusterEvent.MemberLeft),
                typeof(ClusterEvent.MemberRemoved),
                typeof(ClusterEvent.MemberExited),
                typeof(ClusterEvent.MemberStatusChange)
            );

            IActorRef self = Self;
            _wampChannel.RealmProxy.Monitor.ConnectionEstablished += (sender, args) =>
            {
                self.Tell(ConnectionState.Connected);
            };
            _wampChannel.RealmProxy.Monitor.ConnectionBroken += (sender, args) =>
            {
                self.Tell(ConnectionState.Disconnected);
            };

            _wampChannel.Open().Wait();
            _stateTopic = _wampChannel.RealmProxy.TopicContainer.GetTopicByUri("cluster.node.state");
            _statsTopic = _wampChannel.RealmProxy.TopicContainer.GetTopicByUri("cluster.node.statistics");
            _wampChannel.RealmProxy.Services.GetSubject<bool>("cluster.node.state.refresh").Subscribe(_ =>
            {
                cluster.SendCurrentClusterState(self);
            });

            Become(Connected);
        }

        protected override void PostStop()
        {
            base.PostStop();

            _wampChannel.Close();
            _wampChannel = null;
        }

        Task PublishStatus(string nodeName, string nodeState)
        {
            return _stateTopic.Publish(new PublishOptions(), new[]
            {
                new ClusterNodeStatus
                {
                    Name = nodeName,
                    State = nodeState
                }
            });
        }
        Task PublishStatistics(NodeStats nodeStatistics)
        {
            return _stateTopic.Publish(new PublishOptions(),
                new[] { nodeStatistics }
            );
        }

        public static Props Create(Uri wampHostUri)
        {
            return Props.Create<NodeMonitor>(wampHostUri);
        }

        enum ConnectionState
        {
            Connected,
            Disconnected
        }

        public class ClusterNodeStatus
        {
            public string Name { get; set; }

            public string State { get; set; }
        }
    }
}
