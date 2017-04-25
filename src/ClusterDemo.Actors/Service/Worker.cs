using Akka.Actor;
using System;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Messages;

    public class Worker
        : ReceiveActorEx
    {
        readonly int _id;
        readonly IActorRef _workerEvents;

        IActorRef _currentDispatcher;


        public Worker(int id, IActorRef workerEvents)
        {
            _id = id;
            _workerEvents = workerEvents;
        }

        public void WaitingForJob()
        {
            Log.Info("Worker {Worker} waiting for job.", Self.Path);
            _workerEvents.Tell(
                new WorkerAvailable(Self)
            );

            Receive<ExecuteJob>(executeJob =>
            {
                Log.Info("Worker {Worker} executing job {JobId}...",
                    Self.Path.Name,
                    executeJob.Id
                );

                // TODO: Call fake work API.
                // For now, just reply after random delay.
                TimeSpan jobExecutionTime = TimeSpan.FromSeconds(
                    new Random().Next(3, 5)
                );
                ScheduleTellSelfOnce(
                    delay: jobExecutionTime,
                    message: new JobCompleted(executeJob.Id, Self, jobExecutionTime,
                        $"Job {executeJob.Id} completed successfully."
                    )
                );

                Become(ExecutingJob);
            });

            // If dispatcher moves or is restarted, re-announce ourselves.
            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                if (_currentDispatcher != null && _currentDispatcher.Equals(dispatcherAvailable.Dispatcher))
                    return;

                _currentDispatcher = dispatcherAvailable.Dispatcher;
                _workerEvents.Tell(
                    new WorkerAvailable(Self)
                );
            });
        }

        void ExecutingJob()
        {
            Receive<JobCompleted>(jobCompleted =>
            {
                Log.Info("Worker {Worker} completed job {JobId}.", Self.Path, jobCompleted.Id);

                _workerEvents.Tell(jobCompleted);

                Become(WaitingForJob);
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            Become(WaitingForJob);
        }

        public static Props Create(int id, IActorRef workerEvents)
        {
            return Props.Create<Worker>(id, workerEvents);
        }
    }
}
