using Akka.Actor;

namespace ClusterDemo.Actors.Service.Messages
{
    public class DispatcherAvailable
    {
        public DispatcherAvailable(IActorRef dispatcher)
        {
            Dispatcher = dispatcher;
        }

        public IActorRef Dispatcher { get; }
    }
}
