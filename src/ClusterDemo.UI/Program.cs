using Microsoft.AspNetCore.Hosting;
using Serilog;
using System;
using System.IO;
using System.Threading;
using WampSharp.V2;
using WampSharp.V2.PubSub;
using WampSharp.V2.Realm;
using System.Collections.Generic;
using WampSharp.Core.Serialization;
using WampSharp.V2.Core.Contracts;

namespace ClusterDemo.UI
{
    static class Program
    {
        static void Main()
        {
            const int webPort = 14500;
            const int wampPort = webPort + 1;

            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            using (IWebHost webHost = CreateWebHost(webPort))
            using (IWampHost wampHost = CreateWampHost(wampPort))
            {
                Console.WriteLine($"Starting web host on port {webPort}...");
                webHost.Start();

                Console.WriteLine($"Starting WAMP host on port {wampPort}...");
                IWampHostedRealm realm = wampHost.RealmContainer.GetRealmByName("ClusterDemo");
                realm.SessionCreated += (sender, args) =>
                {
                    Log.Information("WAMP session {SessionId} created.",
                        args.SessionId
                    );
                };
                realm.SessionClosed += (sender, args) =>
                {
                    Log.Information("WAMP session {SessionId} closed.",
                        args.SessionId
                    );
                };
                wampHost.Open();

                // Well-known topics.
                realm.TopicContainer.CreateTopicByUri("cluster.node-stats", persistent: true);

                // Temporary diagnostic dump of cluster node-stats events.
                realm.TopicContainer.GetTopicByUri("cluster.node-stats").Subscribe(
                    new TopicDumpSubscriber("cluster.node-stats")
                );

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

        static IWebHost CreateWebHost(int port)
        {
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://127.0.0.1:{port}")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();
        }

        class TopicDumpSubscriber
            : IWampRawTopicRouterSubscriber
        {
            public TopicDumpSubscriber(string topicName)
            {
                TopicName = topicName;
            }

            public string TopicName { get; }

            public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, PublishOptions options)
            {
                Log.Information("TopicDumpSubscriber for topic '{Topic}' received event (publication Id {PublicationId}).", publicationId);
            }

            public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, PublishOptions options, TMessage[] arguments)
            {
                Log.Information("TopicDumpSubscriber for topic '{Topic}' received event (publication Id {PublicationId}): {Arguments}",
                    publicationId,
                    String.Join(",", arguments)
                );
            }

            public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, PublishOptions options, TMessage[] arguments, IDictionary<string, TMessage> argumentsKeywords)
            {
                Log.Information("TopicDumpSubscriber for topic '{Topic}' received event (publication Id {PublicationId}): {Arguments}",
                    publicationId,
                    String.Join(",", arguments)
                );
            }
        }
    }
}
