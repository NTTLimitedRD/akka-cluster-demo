using Akka.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using System.Collections.Immutable;

namespace ClusterDemo.Actors.Common
{
    using Messages;

    public abstract class EventBusActor<TEvent>
        : ReceiveActorEx
    {
        protected EventBusActor()
        {
            Receive<Subscribe>(subscribe =>
            {
                ImmutableList<Type> eventTypes = subscribe.EventTypes;
                if (subscribe.EventTypes.IsEmpty)
                    eventTypes = AllEventTypes;

                AddSubscriber(subscribe.Subscriber, eventTypes);

                Sender.Tell(
                    new Subscribed(subscribe.CorrelationId, eventTypes)
                );
            });
            Receive<Unsubscribe>(unsubscribe =>
            {
                if (!unsubscribe.EventTypes.IsEmpty)
                    RemoveSubscriber(unsubscribe.Subscriber, unsubscribe.EventTypes);
                else
                    RemoveSubscriber(unsubscribe.Subscriber);

                Sender.Tell(
                    new Unsubscribed(unsubscribe.CorrelationId, unsubscribe.EventTypes)
                );
            });
            Receive<TEvent>(workerEvent =>
            {
                PublishEvent(workerEvent);
            });
        }

        EventBus Bus { get; } = new EventBus();

        protected abstract ImmutableList<Type> AllEventTypes { get; }

        protected void PublishEvent(TEvent evt)
        {
            Bus.Publish(evt);
        }

        protected void AddSubscriber(IActorRef subscriber, IEnumerable<Type> messageTypes)
        {
            foreach (Type messageType in messageTypes)
                Bus.Subscribe(subscriber, messageType);
        }

        protected void RemoveSubscriber(IActorRef subscriber, IEnumerable<Type> messageTypes)
        {
            foreach (Type messageType in messageTypes)
                Bus.Unsubscribe(subscriber, messageType);
        }

        protected void RemoveSubscriber(IActorRef subscriber)
        {
            Bus.Unsubscribe(subscriber);
        }
        
        protected class EventBus
            : ActorEventBus<TEvent, Type>
        {
            protected override Type GetClassifier(TEvent evt) => evt.GetType();

            protected override bool Classify(TEvent evt, Type classifier) => classifier.IsAssignableFrom(evt.GetType());

            protected override bool IsSubClassification(Type parent, Type child) => parent.IsAssignableFrom(child);

            protected override void Publish(TEvent evt, IActorRef subscriber) => subscriber.Tell(evt);
        }
    }
}
