using Akka.Actor;

namespace ClusterDemo.Actors.Service.Messages
{
    public class JobStarted
        : IWorkerEvent
    {
        public JobStarted(IActorRef worker, int id)
        {
            Worker = worker;
            Id = id;
        }

        public IActorRef Worker { get; }
        public int Id { get; }
    }
}
