using Akka.Actor;
using Akka.Cluster;
using System;
using System.Reactive.Subjects;
using WampSharp.V2.Realm;

namespace WampClusterMonitor
{
	public class ClusterMonitor
		: ReceiveActor
    {
		public ClusterMonitor(IWampHostedRealm realm)
		{
			ISubject<ClusterNodeMessage> messages = realm.Services.GetSubject<ClusterNodeMessage>("cluster-nodes");

			Receive<ClusterEvent.MemberJoined>(joined =>
			{
				Console.WriteLine("[ClusterMonitor] MemberJoined: '{0}'", joined.Member.Address);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = joined.Member.Address.ToString(),
					State = "Joined"
				});
			});
			Receive<ClusterEvent.MemberLeft>(left =>
			{
				Console.WriteLine("[ClusterMonitor] MemberLeft: '{0}'", left.Member.Address);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = left.Member.Address.ToString(),
					State = "Left"
				});
			});
			Receive<ClusterEvent.MemberUp>(up =>
			{
				Console.WriteLine("[ClusterMonitor] MemberUp: '{0}'", up.Member.Address);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = up.Member.Address.ToString(),
					State = "Up"
				});
			});
		}

		protected override void PreStart()
		{
			base.PreStart();

			Cluster.Get(Context.System).Subscribe(Self,
				typeof(ClusterEvent.MemberJoined),
				typeof(ClusterEvent.MemberLeft),
				typeof(ClusterEvent.MemberUp)
			);
		}
	}

	public class ClusterNodeMessage
	{
		public string Name { get; set; }

		public string State { get; set; }
	}

}
