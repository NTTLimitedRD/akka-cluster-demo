using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace TrivialWebApiWithActor.Web
{
    public class GreetController
        : Controller
    {
        readonly IActorRef _greeter;

        public GreetController(IActorRef greeter)
        {
            _greeter = greeter;
        }

        public async Task<IActionResult> Index(string name = "stranger")
        {
            string greeting = await _greeter.Ask<string>(new GreetMe { Name = name });

            return Ok(greeting);
        }
    }
}