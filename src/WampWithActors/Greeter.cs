using Akka.Actor;
using System;

namespace WampWithActors
{
	class Greeter
		: ReceiveActor
	{
		public static readonly Props Create = Props.Create<Greeter>();

		public Greeter()
		{
			Receive<PrintGreet>(greet =>
			{
				Console.WriteLine($"Hello, {greet.Name}!");
			});

			Receive<GreetMe>(greet =>
			{
				Console.WriteLine($"Greeting '{greet.Name}'...");

				// Reply with greeting.
				Sender.Tell($"Hello, {greet.Name}!");
			});
		}
	}

	public class PrintGreet
	{
		public string Name { get; set; }
	}

	public class GreetMe
	{
		public string Name { get; set; }
	}
}