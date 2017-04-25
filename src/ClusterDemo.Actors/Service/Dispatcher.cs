using Akka.Actor;
using Akka.Cluster.Tools.Client;
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
        readonly IActorRef _workerEvents;

        int _nextJobId = 1;
        PubSub.DistributedPubSub _pubSub;
        ICancelable _dispatchCancellation;

        public Dispatcher(IActorRef workerEvents)
        {
            _workerEvents = workerEvents;
            _pubSub = PubSub.DistributedPubSub.Get(Context.System);
        }

        void Ready()
        {
            Receive<CreateJob>(createJob =>
            {
                int jobId = _nextJobId++;

                Log.Info("Dispatcher is creating job {JobId}.", jobId);

                _pendingJobs.Enqueue(new Job(
                    id: jobId,
                    name: createJob.Name
                 ));
                ScheduleDispatch();
            });
            Receive<WorkerAvailable>(workerAvailable =>
            {
                Log.Info("Dispatcher has available worker {Worker}.", workerAvailable.Worker);

                _availableWorkers.Enqueue(workerAvailable.Worker);

                ScheduleDispatch();
            });
            Receive<Dispatch>(_ =>
            {
                Log.Info("Dispatcher is beginning dispatch cycle ({AvailableWorkerCount} workers, {PendingJobCount} jobs).",
                    _availableWorkers.Count,
                    _pendingJobs.Count
                );

                while (_pendingJobs.Count > 0 && _availableWorkers.Count > 0)
                {
                    Job pendingJob = _pendingJobs.Dequeue();
                    IActorRef worker = _availableWorkers.Dequeue();

                    Log.Info("Dispatcher is dispatching job {JobId} to worker {Worker}.",
                        pendingJob.Id,
                        worker.Path
                    );

                    worker.Tell(new ExecuteJob(
                        id: pendingJob.Id,
                        name: pendingJob.Name
                    ));
                    _activeJobsByWorker[worker] = pendingJob.Id;
                    Context.Watch(worker);

                    ICancelable timeout = ScheduleJobTimeout(pendingJob.Id);

                    _activeJobs.Add(pendingJob.Id,
                        pendingJob.WithWorker(worker, timeout)
                    );
                }

                _dispatchCancellation = null;

                Log.Info("Dispatcher has completed dispatch cycle.");
            });
            Receive<JobCompleted>(jobCompleted =>
            {
                if (Sender == Self)
                    return; // BUG: Message is delivered twice (probably due to new worker event bus logic). FIXME!

                Log.Info("Worker {Worker} reports job {JobId} is complete.",
                    jobCompleted.Worker.Path,
                    jobCompleted.Id
                );

                Job job = _activeJobs[jobCompleted.Id];
                job.Timeout.Cancel();

                _activeJobsByWorker.Remove(jobCompleted.Worker);
                _activeJobs.Remove(jobCompleted.Id);
                Context.Unwatch(jobCompleted.Worker);
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

            Log.Info("Dispatcher started: {Dispatcher}",
                Self.Path.ToStringWithAddress()
            );

            // Subscribe to worker events (local and remote).
            _workerEvents.Tell(
                new Subscribe(Self, eventTypes: new[]
                {
                    typeof(WorkerAvailable),
                    typeof(JobStarted),
                    typeof(JobCompleted)
                })
            );
            _pubSub.Subscribe("worker", Self);

            // Register so we're available to clients outside the cluster.
            ClusterClientReceptionist receptionist = ClusterClientReceptionist.Get(Context.System);
            receptionist.RegisterSubscriber("dispatcher", Self);

            // Tell interested parties that a Dispatcher is now available.
            _pubSub.Publish("dispatcher",
                new DispatcherAvailable(Self)
            );

            // Periodically republish our availability.
            Context.System.Scheduler.ScheduleTellRepeatedly(
                initialDelay: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromSeconds(10),
                receiver: _pubSub.Mediator,
                message: new PubSub.Publish("dispatcher",
                    new DispatcherAvailable(Self)
                ),
                sender: Self
            );

            Become(Ready);
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

        public static Props Create(IActorRef workerEvents)
        {
            return Props.Create<Dispatcher>(workerEvents);
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
