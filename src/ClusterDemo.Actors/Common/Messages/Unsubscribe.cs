using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ClusterDemo.Actors.Common.Messages
{
    public class Unsubscribe
    {
        public Unsubscribe(IActorRef subscriber, IEnumerable<Type> eventTypes = null, string correlationId = null)
        {
            Subscriber = subscriber;
            EventTypes = eventTypes != null ? ImmutableList.CreateRange(eventTypes) : ImmutableList<Type>.Empty;
            CorrelationId = correlationId;
        }

        public IActorRef Subscriber { get; }
        public ImmutableList<Type> EventTypes { get; }
        public string CorrelationId { get; }
    }
}
