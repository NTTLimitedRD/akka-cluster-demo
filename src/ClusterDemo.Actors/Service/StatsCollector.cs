using Akka.Actor;
using System;
using System.Collections.Generic;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Common.Messages;
    using Messages;

    // TODO: Store creation, start, and end times for each job. This will make it easier to calculate stats.
    // TODO: Dispatcher should probably calculate and publish dispatcher stats (whether by itself or via a child actor).

    public class StatsCollector
        : ReceiveActorEx
    {
        readonly HashSet<IActorRef> _allWorkers = new HashSet<IActorRef>();
        readonly HashSet<IActorRef> _availableWorkers = new HashSet<IActorRef>();
        readonly HashSet<IActorRef> _activeWorkers = new HashSet<IActorRef>();

        readonly IActorRef          _nodeMonitor;
        readonly IActorRef          _workerEvents;
        readonly Address            _localNodeAddress;

        // TODO: These value are wrong because we can't reliably know them (ditto for actual worker counts).
        // Instead, have the dispatcher determine and publish them.
        TimeSpan _averageJobExecutionTime;
        TimeSpan _averageJobTurnaroundTime; // TODO: Calculate this as the period between job start and end times.

        public StatsCollector(IActorRef nodeMonitor, IActorRef workerEvents, Address localNodeAddress)
        {
            _nodeMonitor = nodeMonitor;
            _workerEvents = workerEvents;
            _localNodeAddress = localNodeAddress;

            Receive<WorkerAvailable>(workerAvailable =>
            {
                _allWorkers.Add(workerAvailable.Worker);
                _availableWorkers.Add(workerAvailable.Worker);
            });
            Receive<JobStarted>(jobStarted =>
            {
                _availableWorkers.Remove(jobStarted.Worker);
                _activeWorkers.Add(jobStarted.Worker);
            });
            Receive<JobCompleted>(jobCompleted =>
            {
                _activeWorkers.Remove(jobCompleted.Worker);

                // TODO: Consider using a running average instead.
                _averageJobExecutionTime = TimeSpan.FromTicks(
                    (_averageJobExecutionTime.Ticks + jobCompleted.JobExecutionTime.Ticks) / 2
                );
            });

            Receive<PublishStats>(_ =>
            {
                _nodeMonitor.Tell(new NodeStats(
                    nodeAddress: _localNodeAddress.ToString(),
                    totalWorkerCount: _allWorkers.Count,
                    availableWorkerCount: _availableWorkers.Count,
                    activeWorkerCount: _activeWorkers.Count,
                    averageJobExecutionTime: _averageJobExecutionTime,
                    averageJobTurnaroundTime: _averageJobTurnaroundTime
                ));
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            _workerEvents.Tell(
                new Subscribe(Self, eventTypes: new[]
                {
                    typeof(WorkerAvailable),
                    typeof(JobStarted),
                    typeof(JobCompleted)
                })
            );

            ScheduleTellSelfRepeatedly(
                interval: TimeSpan.FromSeconds(3),
                message: PublishStats.Instance
            );
        }

        public static Props Create(IActorRef nodeMonitor, IActorRef workerEvents, Address localNodeAddress)
        {
            return Props.Create<StatsCollector>(nodeMonitor, workerEvents, localNodeAddress);
        }

        class PublishStats
        {
            public static readonly PublishStats Instance = new PublishStats();

            PublishStats()
            {
            }
        }
    }
}
