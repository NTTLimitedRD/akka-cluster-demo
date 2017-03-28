using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading;
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

			int akkaPort = Int32.Parse(args[0]);
			int webPort = akkaPort + 100;
			int wampPort = akkaPort + 200;

			SynchronizationContext.SetSynchronizationContext(
				new SynchronizationContext()
			);

			using (IWebHost webHost = CreateWebHost(webPort))
			using (IWampHost wampHost = CreateWampHost(wampPort))
			using (ActorSystem system = CreateCluster(akkaPort))
			{
				IWampHostedRealm realm = wampHost.RealmContainer.GetRealmByName("ClusterMonitor");

				Console.WriteLine($"Starting web host on port {akkaPort + 100}...");
				webHost.Start();

				Console.WriteLine($"Starting WAMP host on port {akkaPort + 200}...");
				wampHost.Open();

				system.ActorOf(Props.Create(
					() => new ClusterMonitor(realm)
				));

				Console.WriteLine("Running (press enter to terminate).");
				Console.ReadLine();

				Console.WriteLine("Leaving cluster and shutting down...");
				system.ActorOf<Terminator>();

				system.WhenTerminated.Wait();
			}

			Console.WriteLine("Shutdown complete.");
		}

		static IWampHost CreateWampHost(int port)
		{
			return new DefaultWampHost($"ws://127.0.0.1:{port}");
		}

		static IWebHost CreateWebHost(int port)
		{
			return new WebHostBuilder()
				.UseKestrel()
				.UseUrls($"http://+:{port}")
				.UseContentRoot(Directory.GetCurrentDirectory())
				.UseStartup<Startup>()
				.Build();
		}

		static ActorSystem CreateCluster(int port)
		{
			Config config =
				ConfigurationFactory.ParseString(
					$"akka.remote.helios.tcp.port={port}"
				)
				.WithFallback(BaseConfig);

			return ActorSystem.Create("Cluster", config);
		}
	}
}