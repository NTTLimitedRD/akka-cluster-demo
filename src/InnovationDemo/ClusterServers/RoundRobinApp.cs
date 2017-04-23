using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;

namespace ClusterServers
{
    public class RoundRobinApp
    {
        public static IActorRef RoundRobinGroup;

        static RoundRobinApp()
        {
            using (var system = ActorSystem.Create("MySystem"))
            {
                for (int workerId = 1; workerId <= 4; workerId++)
                {
                    system.ActorOf(
                        Worker.Create(workerId),
                        name: $"Worker{workerId}"
                    );
                }

                var config = ConfigurationFactory.ParseString(@"
routees.paths = [
    ""akka://MySystem/user/Worker1"" #testing full path
    user/Worker2
    user/Worker3
    user/Worker4
]");

                RoundRobinGroup = system.ActorOf(Props.Empty.WithRouter(new RoundRobinGroup(config)));
            }
        }
    }
}
