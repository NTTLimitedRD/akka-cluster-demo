using Akka.Actor;
using Akka.Configuration;
using System;
using System.Reactive;
using System.Threading;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace WampWithActors
{
	static class Program
	{
		static readonly Config DefaultConfig = ConfigurationFactory.ParseString(@"
            akka {
                suppress-json-serializer-warning = on
            }
        ");

		static void Main()
		{
			try
			{
				SynchronizationContext.SetSynchronizationContext(
					new SynchronizationContext()
				);

				using (var system = ActorSystem.Create("Demo", DefaultConfig))
				{
					IActorRef greeter = system.ActorOf(Greeter.Create);
					greeter.Tell(new PrintGreet
					{
						Name = "World"
					});

					Console.WriteLine("Creating host...");
					using (IWampHost host = CreateHost(greeter))
					{
						IWampHostedRealm realm = host.RealmContainer.GetRealmByName("Actors");

						Console.WriteLine("Starting host...");
						host.Open();

						Console.WriteLine("Running (press enter to terminate).");
						Console.ReadLine();

						Console.WriteLine("Stopping host...");
					}

					Console.WriteLine("Shutting down...");
					system.Terminate().Wait();
				}
				Console.WriteLine("Shutdown complete.");
			}
			catch (Exception unexpectedError)
			{
				Console.WriteLine(unexpectedError);
			}
		}

		static IWampHost CreateHost(IActorRef greeter)
		{
			IWampHost host = new DefaultWampHost("ws://127.0.0.1:19100");
			IWampHostedRealm realm = host.RealmContainer.GetRealmByName("Actors");
			realm.Services.GetSubject<string>("names").Subscribe(name =>
			{
				greeter.Tell(new GreetMe
				{
					Name = name
				});
			});

			return host;
		}
	}
}