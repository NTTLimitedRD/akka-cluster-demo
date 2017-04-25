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

            ConfigureLogging();

            try
            {
                using (IWebHost webHost = CreateWebHost(webPort))
                using (IWampHost wampHost = CreateWampHost(wampPort))
                {
                    Log.Information("Starting web host on port {WebPort}...", webPort);
                    webHost.Start();

                    Log.Information("Starting WAMP host on port {WampPort}...", wampPort);
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
                    realm.TopicContainer.CreateTopicByUri("cluster.node.state", persistent: true);
                    realm.TopicContainer.CreateTopicByUri("cluster.node.statistics", persistent: true);
                    realm.TopicContainer.CreateTopicByUri("cluster.node.state.refresh", persistent: true);

                    Log.Information("Running (press enter to terminate).");
                    Console.ReadLine();

                    Log.Information("Shutting down...");
                }

                Log.Information("Shutdown complete.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
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

        static void ConfigureLogging()
        {
            string[] commandLine = Environment.GetCommandLineArgs();
            commandLine[0] = Path.GetFileNameWithoutExtension(commandLine[0]);

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty(
                    name: "Program",
                    value: String.Join(" ", commandLine)
                )
                .WriteTo.Seq("http://localhost:5341/",
                    apiKey: "MHXFLikIgkbTIsfJuTYP",
                    period: TimeSpan.FromSeconds(1)
                )
                .WriteTo.ColoredConsole(
                    outputTemplate: "[{Level}] {Message}{NewLine}{Exception}"
                )
                .CreateLogger();
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
