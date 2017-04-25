using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WampSharp.V2;
using WampSharp.V2.Client;

namespace ClusterDemo.Actors.Service
{
    using Akka.Actor;
    using Common;
    using Messages;
    using WampSharp.V2.Core.Contracts;

    public class NodeMonitor
        : ReceiveActorEx
    {
        public static readonly string ActorName = "node-monitor";

        readonly Uri    _wampHostUri;
        IWampChannel    _wampChannel;
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
                    _statsTopic = _wampChannel.RealmProxy.TopicContainer.GetTopicByUri("cluster.node-stats");

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
            _statsTopic = _wampChannel.RealmProxy.TopicContainer.GetTopicByUri("cluster.node-stats");

            Become(Connected);
        }

        protected override void PostStop()
        {
            base.PostStop();

            _wampChannel.Close();
            _wampChannel = null;
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
    }
}
