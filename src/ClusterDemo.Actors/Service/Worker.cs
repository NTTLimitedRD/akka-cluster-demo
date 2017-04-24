using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    using Messages;
    using System.Threading;

    public class Worker
        : ReceiveActorEx
    {
        readonly int _id;
        IActorRef _dispatcher;

        public Worker(int id, IActorRef dispatcher)
        {
            _id = id;
            _dispatcher = dispatcher;

            Become(WaitingForJob);
        }

        public void WaitingForJob()
        {
            Receive<DispatcherAvailable>(dispatcherAvailable =>
            {
                _dispatcher = dispatcherAvailable.Dispatcher;
            });
            Receive<ExecuteJob>(executeJob =>
            {
                Log.Info("Worker {Worker} executing job {JobId}...",
                    Self.Path.Name,
                    executeJob.Id
                );

                // TODO: Call fake work API.

                // For now, just reply after random delay.
                Context.System.Scheduler.ScheduleTellOnce(
                    delay: TimeSpan.FromSeconds(
                        new Random().Next(3, 5)
                    ),
                    receiver: Sender,
                    message: new JobCompleted(executeJob.Id,
                        $"Job {executeJob.Id} completed successfully."
                    ),
                    sender: Self
                );
            });
        }
    }
}
