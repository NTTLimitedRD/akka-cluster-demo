using Akka.Actor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace TrivialWebApiWithActor.Web
{
    public class GreetController
        : Controller
    {
        readonly Actors _actors;

        public GreetController(ILogger<GreetController> logger, Actors actors)
        {
            Log = logger;
            _actors = actors;
        }

        ILogger Log { get; }

        public async Task<IActionResult> Index(string name = "stranger")
        {
            Log.LogInformation("Greeting '{Name}'...", name);

            string greeting = await _actors.Greeter.Ask<string>(new GreetMe { Name = name });

            return Ok(greeting);
        }
    }
}