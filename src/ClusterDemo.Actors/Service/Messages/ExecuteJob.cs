namespace ClusterDemo.Actors.Service.Messages
{
    public class ExecuteJob
    {
        public ExecuteJob(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }
        public string Name { get; }
    }
}
