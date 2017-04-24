namespace ClusterDemo.Actors.Common.Messages
{
    public class JobCreated
    {
        public JobCreated(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }

        public int Id { get; }
    }
}
