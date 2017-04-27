using Akka.Actor;
using Akka.Actor.Dsl;
using System;

namespace ClusterDemo.Actors
{
    /// <summary>
    ///		Helper methods for inline supervision.
    /// </summary>
    public static class InlineSupervision
    {
        /// <summary>
        ///		Create an actor with inline supervision.
        /// </summary>
        /// <param name="actorRefFactory">
        ///		The <see cref="IActorRefFactory"/> used to create the actor.
        /// </param>
        /// <param name="props">
        ///		<see cref="Props"/> used to construct the actor.
        /// </param>
        /// <param name="inlineSupervisorStrategy">
        ///		The inline supervisor strategy used to supervise the actor.
        /// </param>
        /// <param name="name">
        ///		An optional name for the actor.
        /// </param>
        /// <returns>
        ///		A reference to the actor's supervisor (the supervisor will forward all messages to the supervised actor).
        /// </returns>
        public static IActorRef SupervisedActorOf(this IActorRefFactory actorRefFactory, Props props, SupervisorStrategy inlineSupervisorStrategy, string name = null)
        {
            if (actorRefFactory == null)
                throw new ArgumentNullException(nameof(actorRefFactory));

            if (inlineSupervisorStrategy == null)
                throw new ArgumentNullException(nameof(inlineSupervisorStrategy));

            return actorRefFactory.ActorOf(actor =>
            {
                actor.Strategy = inlineSupervisorStrategy;

                IActorRef supervisedActor = null;

                actor.OnPreStart = context =>
                {
                    supervisedActor = context.ActorOf(props, name);

                    context.Watch(supervisedActor);
                };

                actor.Receive<Terminated>(terminated => terminated.ActorRef.Equals(supervisedActor),
                    (terminated, context) => context.Stop(context.Self)
                );
                actor.ReceiveAny((message, context) =>
                {
                    if (ReferenceEquals(context.Sender, supervisedActor))
                        context.Parent.Forward(message); // Act as if the supervised actor was directly under the supervisor's parent.
                    else
                        supervisedActor.Forward(message);
                });
            }, name: name + "-supervisor");
        }
    }
}
