using Akka.Actor;
using System;

namespace ClusterDemo.Actors.Service
{
    using Common;
    using Messages;

    public class StatsCollector
        : ReceiveActorEx
    {
        readonly IActorRef _nodeMonitor;
        readonly IActorRef _workerEvents;
        
        int _availableWorkerCount;
        int _activeWorkerCount;
        TimeSpan _averageJobExecutionTime;
        TimeSpan _averageJobTurnaroundTime; // TODO: Calculate this as the period between job start and end times.

        public StatsCollector(IActorRef nodeMonitor, IActorRef workerEvents)
        {
            _nodeMonitor = nodeMonitor;
            _workerEvents = workerEvents;

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
                    nodeAddress: Self.Path.Address.ToString(),
                    availableWorkerCount: _availableWorkerCount,
                    activeWorkerCount: _activeWorkerCount,
                    averageJobExecutionTime: _averageJobExecutionTime
                ));
            });
        }

        protected override void PreStart()
        {
            base.PreStart();

            ScheduleTellSelfRepeatedly(
                interval: TimeSpan.FromSeconds(1),
                message: PublishStats.Instance
            );
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
