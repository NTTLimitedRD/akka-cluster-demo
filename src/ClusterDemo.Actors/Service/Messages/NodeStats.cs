using System;

namespace ClusterDemo.Actors.Service.Messages
{
    public class NodeStats
    {
        public NodeStats(string nodeAddress, int availableWorkerCount, int activeWorkerCount, TimeSpan averageJobExecutionTime)
        {
            NodeAddress = nodeAddress;
            AvailableWorkerCount = availableWorkerCount;
            ActiveWorkerCount = activeWorkerCount;
            AverageJobExecutionTime = averageJobExecutionTime;
        }

        public string NodeAddress { get; }
        public int AvailableWorkerCount { get; }
        public int ActiveWorkerCount { get; }
        public TimeSpan AverageJobExecutionTime { get; }
    }
}
