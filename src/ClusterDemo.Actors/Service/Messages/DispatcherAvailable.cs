using Akka.Actor;

namespace ClusterDemo.Actors.Service.Messages
{
    public class DispatcherAvailable
    {
        public DispatcherAvailable(IActorRef dispatcher, bool isFirstAnnouncement = true)
        {
            Dispatcher = dispatcher;
            IsFirstAnnouncement = isFirstAnnouncement;
        }

        public IActorRef Dispatcher { get; }

        public bool IsFirstAnnouncement { get; }
    }
}
