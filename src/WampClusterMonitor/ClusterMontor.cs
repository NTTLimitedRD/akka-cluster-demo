using Akka.Actor;
using Akka.Cluster;
using System.Reactive.Subjects;
using WampSharp.V2.Realm;

namespace WampClusterMonitor
{
	public class ClusterMontor
		: ReceiveActor
    {
		public ClusterMontor(IWampHostedRealm realm)
		{
			ISubject<ClusterNodeMessage> messages = realm.Services.GetSubject<ClusterNodeMessage>("cluster-nodes");
			Receive<ClusterEvent.MemberJoined>(joined =>
			{
				messages.OnNext(new ClusterNodeMessage
				{
					Name = joined.Member.Address.ToString(),
					State = "Joined"
				});
			});
			Receive<ClusterEvent.MemberLeft>(joined =>
			{
				messages.OnNext(new ClusterNodeMessage
				{
					Name = joined.Member.Address.ToString(),
					State = "Left"
				});
			});
			Receive<ClusterEvent.MemberUp>(joined =>
			{
				messages.OnNext(new ClusterNodeMessage
				{
					Name = joined.Member.Address.ToString(),
					State = "Up"
				});
			});
		}
	}

	public class ClusterNodeMessage
	{
		public string Name { get; set; }

		public string State { get; set; }
	}

}
