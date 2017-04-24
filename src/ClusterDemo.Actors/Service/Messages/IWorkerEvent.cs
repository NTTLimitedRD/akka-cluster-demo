using Akka.Actor;

namespace ClusterDemo.Actors.Service.Messages
{
    public interface IWorkerEvent
    {
        IActorRef Worker { get; }
    }
}
