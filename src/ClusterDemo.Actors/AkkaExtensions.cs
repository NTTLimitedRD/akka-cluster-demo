using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using PubSub = Akka.Cluster.Tools.PublishSubscribe;

namespace ClusterDemo.Actors
{
    public static class AkkaExtensions
    {
        public static void Publish(this PubSub.DistributedPubSub pubSub, string topic, object message, bool sendOneMessageToEachGroup = false)
        {
            if (pubSub == null)
                throw new ArgumentNullException(nameof(pubSub));

            pubSub.Mediator.Tell(
                new PubSub.Publish(topic, message, sendOneMessageToEachGroup)
            );
        }

        public static void Subscribe(this PubSub.DistributedPubSub pubSub, string topic, IActorRef subject, string group = null)
        {
            if (pubSub == null)
                throw new ArgumentNullException(nameof(pubSub));

            pubSub.Mediator.Tell(
                new PubSub.Subscribe(topic, subject, group)
            );
        }

        public static void Unsubscribe(this PubSub.DistributedPubSub pubSub, string topic, IActorRef subject, string group = null)
        {
            if (pubSub == null)
                throw new ArgumentNullException(nameof(pubSub));

            pubSub.Mediator.Tell(
                new PubSub.Unsubscribe(topic, subject, group)
            );
        }
    }
}
