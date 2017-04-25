using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Messages;

    public class Worker
        : ReceiveActorEx
    {
        readonly int _id;
        IActorRef _dispatcher;

        public Worker(int id, IActorRef dispatcher)
        {
            _id = id;
            _dispatcher = dispatcher;
        }

        public void WaitingForJob()
        {
            _dispatcher.Tell(
                new WorkerAvailable(Self)
            );

            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                if (_dispatcher.Equals(dispatcherAvailable.Dispatcher))
                    return;

                _dispatcher = dispatcherAvailable.Dispatcher;

                Become(WaitingForJob);
            });
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
        }

        void ExecutingJob()
        {
            Receive<JobCompleted>(jobCompleted =>
            {
                _dispatcher.Tell(jobCompleted);

                Become(WaitingForJob);
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            Become(WaitingForJob);
        }

        public static Props Create(int id, IActorRef dispatcher)
        {
            return Props.Create<Worker>(id, dispatcher);
        }
    }
}
