using Akka.Actor;
using System;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Common.Messages;
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

        void WaitingForDispatcher()
        {
            Log.Info("Worker {Worker} waiting for initial announcement from dispatcher.", Self.Path);

            // If dispatcher moves or is restarted, re-announce ourselves.
            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                Log.Info("Worker {Worker} received initial announcement from dispatcher {Dispatcher}.",
                    Self.Path,
                    dispatcherAvailable.Dispatcher.Path.ToStringWithAddress()
                );

                _currentDispatcher = dispatcherAvailable.Dispatcher;

                Become(WaitingForJob);
            });
        }

        void WaitingForJob()
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
                    new Random().Next(1, 10)
                );
                ScheduleTellSelfOnce(
                    delay: jobExecutionTime,
                    message: new JobCompleted(executeJob.Id, Self, jobExecutionTime,
                        $"Job {executeJob.Id} completed successfully."
                    )
                );

                _workerEvents.Tell(new JobStarted(
                    worker: Self,
                    id: executeJob.Id
                ));

                Become(ExecutingJob);
            });

            // If dispatcher moves or is restarted, re-announce ourselves.
            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                if (_currentDispatcher.Equals(dispatcherAvailable.Dispatcher) && !dispatcherAvailable.IsFirstAnnouncement)
                    return; // We already know about this dispatcher; no need to re-announce ourselves.

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

            Become(WaitingForDispatcher);
        }

        public static Props Create(int id, IActorRef workerEvents)
        {
            return Props.Create<Worker>(id, workerEvents);
        }
    }
}
