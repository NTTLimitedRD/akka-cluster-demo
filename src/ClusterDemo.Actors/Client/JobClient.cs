using Akka.Cluster.Tools.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Client
{
    using Akka.Actor;
    using Common;
    using Common.Messages;

    public class JobClient
        : ReceiveActorEx
    {
        readonly Queue<CreateJob> _pendingJobs = new Queue<CreateJob>();

        IActorRef   _clusterClient;
        IActorRef   _dispatcher;
        ICancelable _findDispatcherCancellation;

        public JobClient(IActorRef clusterClient)
        {
            _clusterClient = clusterClient;
        }

        void FindDispatcher()
        {
            Log.Info("JobClient is searching for dispatcher...");

            _findDispatcherCancellation = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
                initialDelay: TimeSpan.Zero,
                interval: TimeSpan.FromSeconds(5),
                receiver: _clusterClient,
                message: new ClusterClient.Publish("dispatcher",
                    new Identify("dispatcher")
                ),
                sender: Self
            );
            
            Receive<ActorIdentity>(dispatcherIdentity =>
            {
                if (dispatcherIdentity.MessageId as string != "dispatcher")
                {
                    Unhandled(dispatcherIdentity);

                    return;
                }

                if (dispatcherIdentity.Subject == null)
                    return;

                _dispatcher = dispatcherIdentity.Subject;
                _findDispatcherCancellation.Cancel();
                _findDispatcherCancellation = null;

                Become(Ready);
            });

            // Locally queue pending jobs.
            Receive<CreateJob>(createJob =>
            {
                _pendingJobs.Enqueue(createJob);
            });
        }

        void Ready()
        {
            int localJobCount = _pendingJobs.Count;
            Log.Info("JobClient has found dispatcher ({LocallyQueuedJobCount} jobs queued locally).", localJobCount);

            // Send pending jobs.
            while (_pendingJobs.Count > 0)
            {
                _dispatcher.Tell(
                    _pendingJobs.Dequeue()
                );
            }

            Log.Info("JobClient submitted {LocallyQueuedJobCount} locally-queued jobs.", localJobCount);

            Receive<CreateJob>(createJob =>
            {
                Log.Info("JobClient submitting job '{JobName}'.", createJob.Name);

                _dispatcher.Tell(createJob);

                Log.Info("JobClient submitted job '{JobName}'.", createJob.Name);
            });

            // Periodically refresh our view of the dispatcher in case it's moved.
            ScheduleTellSelfOnce(TimeSpan.FromSeconds(10), RefreshDispatcher.Instance);
            Receive<RefreshDispatcher>(_ =>
            {
                Become(FindDispatcher);
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            Log.Info("JobClient started.");

            Become(FindDispatcher);
        }

        protected override void PostStop()
        {
            base.PostStop();

            Log.Info("JobClient stopped.");
        }

        class RefreshDispatcher
        {
            public static readonly RefreshDispatcher Instance = new RefreshDispatcher();

            RefreshDispatcher()
            {
            }
        }
    }
}
