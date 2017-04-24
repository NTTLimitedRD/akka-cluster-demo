using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    using Common.Messages;
    using Messages;

    public class Dispatcher
        : ReceiveActorEx
    {
        readonly Queue<Job> _pendingJobs = new Queue<Job>();
        readonly Queue<IActorRef> _availableWorkers = new Queue<IActorRef>();
        readonly Dictionary<int, Job> _activeJobs = new Dictionary<int, Job>();

        int _nextJobId = 1;
        ICancelable _dispatchCancellation;

        public Dispatcher()
        {
            Receive<CreateJob>(createJob =>
            {
                int jobId = _nextJobId++;

                _pendingJobs.Enqueue(new Job(
                    id: jobId,
                    name: createJob.Name
                 ));
                ScheduleDispatch();
            });
            Receive<Dispatch>(_ =>
            {
                while (_pendingJobs.Count > 0 && _availableWorkers.Count > 0)
                {
                    Job pendingJob = _pendingJobs.Dequeue();
                    IActorRef worker = _availableWorkers.Dequeue();

                    worker.Tell(new ExecuteJob(
                        id: pendingJob.Id,
                        name: pendingJob.Name
                    ));

                    ICancelable timeout = Context.System.Scheduler.ScheduleTellOnceCancelable(
                        delay: TimeSpan.FromSeconds(10),
                        receiver: Self,
                        message: new JobTimeout(pendingJob.Id),
                        sender: Self
                    );

                    _activeJobs.Add(pendingJob.Id,
                        pendingJob.WithWorker(worker, timeout)
                    );
                }
            });
            Receive<JobTimeout>(jobTimeout =>
            {
                Job job = _activeJobs[jobTimeout.Id];

                // TODO: Log and handle job timeout.
            });
        }

        void ScheduleDispatch()
        {
            if (_dispatchCancellation != null)
                return;

            _dispatchCancellation = Context.System.Scheduler.ScheduleTellOnceCancelable(
                delay: TimeSpan.FromSeconds(1),
                receiver: Self,
                message: Dispatch.Instance,
                sender: Self
             );
        }

        class Dispatch
        {
            public static readonly Dispatch Instance = new Dispatch();

            Dispatch()
            {
            }
        }

        class JobTimeout
        {
            public JobTimeout(int id)
            {
                Id = id;
            }

            public int Id { get; }
        }

        class Job
        {
            public Job(int id, string name)
            {
                Id = id;
                Name = name;
            }

            Job(int id, string name, IActorRef worker, ICancelable timeout)
            {
                Id = id;
                Name = name;
                Worker = worker;
                Timeout = timeout;
            }

            public int Id { get; }
            public string Name { get; }
            public IActorRef Worker { get; }
            public ICancelable Timeout { get; }

            public Job WithWorker(IActorRef worker, ICancelable timeout)
            {
                return new Job(Id, Name, worker, timeout);
            }
        }
    }
}
