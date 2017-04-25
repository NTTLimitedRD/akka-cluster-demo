using Akka.Actor;
using System;

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
        readonly IActorRef  _nodeMonitor;
        readonly IActorRef  _workerEvents;
        readonly Address    _localNodeAddress;
        
        int _availableWorkerCount;
        int _activeWorkerCount;
        TimeSpan _averageJobExecutionTime;
        TimeSpan _averageJobTurnaroundTime; // TODO: Calculate this as the period between job start and end times.

        public StatsCollector(IActorRef nodeMonitor, IActorRef workerEvents, Address localNodeAddress)
        {
            _nodeMonitor = nodeMonitor;
            _workerEvents = workerEvents;
            _localNodeAddress = localNodeAddress;

            Receive<WorkerAvailable>(workerAvailable =>
            {
                _availableWorkerCount++;
            });
            Receive<JobStarted>(jobStarted =>
            {
                _availableWorkerCount--;
                _activeWorkerCount++;
            });
            Receive<JobCompleted>(jobCompleted =>
            {
                _activeWorkerCount--;

                // TODO: Consider using a running average instead.
                _averageJobExecutionTime = TimeSpan.FromTicks(
                    (_averageJobExecutionTime.Ticks + jobCompleted.JobExecutionTime.Ticks) / 2
                );
            });

            Receive<PublishStats>(_ =>
            {
                _nodeMonitor.Tell(new NodeStats(
                    nodeAddress: _localNodeAddress.ToString(),
                    availableWorkerCount: _availableWorkerCount,
                    activeWorkerCount: _activeWorkerCount,
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
                    typeof(JobCreated),
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
