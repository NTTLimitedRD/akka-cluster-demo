using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace TrivialWebApiWithActor.Web
{
    public class GreetController
        : Controller
    {
        readonly IActorRef _greeter;

        public GreetController(ILogger<GreetController> logger, IActorRef greeter)
        {
            Log = logger;
            _greeter = greeter;
        }

        ILogger Log { get; }

        public async Task<IActionResult> Index(string name = "stranger")
        {
            Log.LogInformation("Greeting '{Name}'...", name);

            string greeting = await _greeter.Ask<string>(new GreetMe { Name = name });

            return Ok(greeting);
        }
    }
}