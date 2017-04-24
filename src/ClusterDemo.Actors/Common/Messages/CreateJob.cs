namespace ClusterDemo.Actors.Common.Messages
{
    public sealed class CreateJob
    {
        public CreateJob(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
