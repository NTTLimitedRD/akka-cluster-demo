using Akka.Actor;
using System;
using System.Collections.Generic;

using PubSub = Akka.Cluster.Tools.PublishSubscribe;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Common.Messages;
    using Messages;

    public class Dispatcher
        : ReceiveActorEx
    {
        public static readonly string ActorName = "dispatcher";

        readonly Queue<Job> _pendingJobs = new Queue<Job>();
        readonly Queue<IActorRef> _availableWorkers = new Queue<IActorRef>();
        readonly Dictionary<int, Job> _activeJobs = new Dictionary<int, Job>();
        readonly Dictionary<IActorRef, int> _activeJobsByWorker = new Dictionary<IActorRef, int>();

        int _nextJobId = 1;
        IActorRef _pubSub;
        ICancelable _dispatchCancellation;

        public Dispatcher()
        {
            // TODO: Use ClusterSingletonManager to ensure that we're only running a single instance of the dispatcher in the cluster.

            Receive<CreateJob>(createJob =>
            {
                int jobId = _nextJobId++;

                _pendingJobs.Enqueue(new Job(
                    id: jobId,
                    name: createJob.Name
                 ));
                ScheduleDispatch();
            });
            Receive<WorkerAvailable>(workerAvailable =>
            {
                _availableWorkers.Enqueue(workerAvailable.Worker);

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
                    _activeJobsByWorker.Add(worker,
                        pendingJob.Id
                    );
                    Context.Watch(worker);

                    ICancelable timeout = ScheduleJobTimeout(pendingJob.Id);

                    _activeJobs.Add(pendingJob.Id,
                        pendingJob.WithWorker(worker, timeout)
                    );
                }
            });
            Receive<JobCompleted>(jobCompleted =>
            {
                Log.Info("Worker {Worker} reports job {JobId} is complete.",
                    Sender.Path,
                    jobCompleted.Id
                );

                Job job = _activeJobs[jobCompleted.Id];
                job.Timeout.Cancel();

                _activeJobsByWorker.Remove(Sender);
                _activeJobs.Remove(jobCompleted.Id);
            });
            Receive<JobTimeout>(jobTimeout =>
            {
                Job job = _activeJobs[jobTimeout.Id];

                Log.Info("Job {JobId} timed out.",
                    job.Id
                );

                _activeJobsByWorker.Remove(Sender);
                _activeJobs.Remove(jobTimeout.Id);

                // TODO: Handle job timeout.
            });
            Receive<Terminated>(terminated =>
            {
                int jobId;
                if (!_activeJobsByWorker.TryGetValue(terminated.ActorRef, out jobId))
                    return;

                Log.Info("Worker {Worker} terminated while processing job {JobId}.",
                    terminated.ActorRef.Path,
                    jobId
                );

                // TODO: Handle worker termination.

                _activeJobsByWorker.Remove(terminated.ActorRef);
                _activeJobs.Remove(jobId);
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            Log.Info("Dispatcher started on node: {DispatcherNodeAddress}",
                Self.Path.Address
            );

            // Tell interested parties that a Dispatcher is now available.
            _pubSub = PubSub.DistributedPubSub.Get(Context.System).Mediator;
            _pubSub.Tell(new PubSub.Send("dispatcher",
                new DispatcherAvailable(Self)
            ));
        }

        void ScheduleDispatch()
        {
            if (_dispatchCancellation != null)
                return;

            _dispatchCancellation = ScheduleTellSelfOnceCancelable(
                delay: TimeSpan.FromSeconds(1),
                message: Dispatch.Instance
             );
        }

        ICancelable ScheduleJobTimeout(int jobId)
        {
            return ScheduleTellSelfOnceCancelable(
                delay: TimeSpan.FromSeconds(10),
                message: new JobTimeout(jobId)
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
