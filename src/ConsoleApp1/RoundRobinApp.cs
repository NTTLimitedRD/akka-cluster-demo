using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;

namespace ConsoleApp1
{
    public class RoundRobinApp
    {
        public static IActorRef RoundRobinGroup;

        static RoundRobinApp()
        {
            using (var system = ActorSystem.Create("MySystem"))
            {


                system.ActorOf<Worker>("Worker1");
                system.ActorOf<Worker>("Worker2");
                system.ActorOf<Worker>("Worker3");
                system.ActorOf<Worker>("Worker4");

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
