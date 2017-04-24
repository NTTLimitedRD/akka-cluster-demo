using Akka.Actor;

namespace ClusterDemo.Actors.Service.Messages
{
    public class WorkerAvailable
    {
        public WorkerAvailable(IActorRef worker)
        {
            Worker = worker;
        }

        public IActorRef Worker { get; }
    }
}
