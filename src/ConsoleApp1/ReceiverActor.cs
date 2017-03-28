using Akka.Actor;
using Akka.Cluster.Routing;
using Akka.Routing;
using Contracts;

namespace ClusterServers
{
    public class ReceiverActor : ReceiveActor
    {
        IActorRef MyRouter { get; set; }

        public ReceiverActor()
        {
            Receive<ProvisionJob>(job =>
            {
                MyRouter.Forward(job);
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            MyRouter = Context.ActorOf(Props.Empty.WithRouter(
                new ClusterRouterGroup(
                    new RoundRobinGroup("/user/worker"),
                    new ClusterRouterGroupSettings(
                        3, true, new[] { "/user/worker" }
                    )
                )
            ), name: "myRouter");
        }
    }
}
