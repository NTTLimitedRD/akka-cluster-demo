using Akka.Actor;
using System;

namespace TrivialWebApiWithActor.Web
{
    public class Actors
    {
        public Actors(IActorRef greeter)
        {
            if (greeter == null)
                throw new ArgumentNullException(nameof(greeter));

            Greeter = greeter;
        }

        public IActorRef Greeter { get; }
    }
}
