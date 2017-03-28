using Akka.Actor;
using Akka.Configuration;
using System;
using System.IO;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace WampClusterMonitor
{
	static class Program
	{
		static readonly Config BaseConfig = ConfigurationFactory.ParseString(@"
			akka {
				suppress-json-serializer-warning = on

				actor {
					provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
				}

				remote {
					helios.tcp {
						hostname = ""127.0.0.1""
					}
				}

				cluster {
					seed-nodes = [
						""akka.tcp://Cluster@127.0.0.1:19410""
					]
				}
			}
		");

		static void Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.WriteLine("Usage:\n\t{0} <ListenPort>",
					Path.GetFileNameWithoutExtension(
						Environment.GetCommandLineArgs()[0]
					)
				);

				return;
			}

			int listenPort = Int32.Parse(args[0]);

			Config config =
				ConfigurationFactory.ParseString(
					$"akka.remote.helios.tcp.port={listenPort}"
				)
				.WithFallback(BaseConfig);

			IWampHost wampHost = listenPort == 19140 ? CreateWampHost(191600) : null;
			using (wampHost)
			using (ActorSystem system = ActorSystem.Create("Cluster", config))
			{
				IWampHostedRealm realm = wampHost.RealmContainer.GetRealmByName("ClusterMonitor");

				if (wampHost != null)
				{
					Console.WriteLine("Starting WAMP host...");

					system.ActorOf(Props.Create(
						() => new ClusterMontor(realm)
					));
				}

				Console.WriteLine("Running (press enter to terminate).");
				Console.ReadLine();

				Console.WriteLine("Shutting down...");
			}

			Console.WriteLine("Shutdown complete.");
		}

		static IWampHost CreateWampHost(int port)
		{
			return new DefaultWampHost($"ws://127.0.0.1:{port}");
		}
	}
}