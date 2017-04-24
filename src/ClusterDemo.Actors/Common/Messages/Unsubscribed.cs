using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ClusterDemo.Actors.Common.Messages
{
    public class Unsubscribed
    {
        public Unsubscribed(string correlationId, IEnumerable<Type> eventTypes)
        {
            CorrelationId = correlationId;
            EventTypes = eventTypes != null ? ImmutableList.CreateRange(eventTypes) : ImmutableList<Type>.Empty;
        }

        public string CorrelationId { get; }
        public ImmutableList<Type> EventTypes { get; }
    }
}
