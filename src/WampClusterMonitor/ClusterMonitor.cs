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
		IDisposable _refreshSubscription;

		public ClusterMonitor(IWampHostedRealm realm)
		{
			ISubject<ClusterNodeMessage> messages = realm.Services.GetSubject<ClusterNodeMessage>("cluster-nodes");
			ISubject<bool> refresh = realm.Services.GetSubject<bool>("refresh-cluster-state");

			Cluster cluster = Cluster.Get(Context.System);
			IActorRef self = Self;
			_refreshSubscription = refresh.Subscribe(_ =>
			{
				cluster.SendCurrentClusterState(self);
			});

			Receive<ClusterEvent.CurrentClusterState>(clusterState =>
			{
				Console.WriteLine("[ClusterMonitor] CurrentClusterState");

				foreach (Member member in clusterState.Members)
				{
					messages.OnNext(new ClusterNodeMessage
					{
						Name = member.Address.ToString(),
						State = member.Status.ToString()
					});
				}
			});
			Receive<ClusterEvent.MemberJoined>(joined =>
			{
				Console.WriteLine("[ClusterMonitor] MemberJoined: '{0}'", joined.Member.Address);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = joined.Member.Address.ToString(),
					State = MemberStatus.Joining.ToString()
				});
			});
			Receive<ClusterEvent.MemberLeft>(left =>
			{
				Console.WriteLine("[ClusterMonitor] MemberLeft: '{0}'", left.Member.Address);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = left.Member.Address.ToString(),
					State = MemberStatus.Leaving.ToString()
				});
			});
			Receive<ClusterEvent.MemberExited>(left =>
			{
				Console.WriteLine("[ClusterMonitor] MemberExited: '{0}'", left.Member.Address);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = left.Member.Address.ToString(),
					State = MemberStatus.Exiting.ToString()
				});
			});
			Receive<ClusterEvent.MemberRemoved>(removed =>
			{
				Console.WriteLine("[ClusterMonitor] MemberLeft: '{0}'", removed.Member.Address);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = removed.Member.Address.ToString(),
					State = MemberStatus.Removed.ToString()
				});
			});
			Receive<ClusterEvent.MemberStatusChange>(statusChange =>
			{
				Console.WriteLine("[ClusterMonitor] MemberUp: '{0}' ({1})",
					statusChange.Member.Address,
					statusChange.Member.Status
				);

				messages.OnNext(new ClusterNodeMessage
				{
					Name = statusChange.Member.Address.ToString(),
					State = statusChange.Member.Status.ToString()
				});
			});
		}

		protected override void PreStart()
		{
			base.PreStart();

			Cluster cluster = Cluster.Get(Context.System);
			cluster.Subscribe(Self,
				typeof(ClusterEvent.MemberJoined),
				typeof(ClusterEvent.MemberLeft),
				typeof(ClusterEvent.MemberRemoved),
				typeof(ClusterEvent.MemberExited),
				typeof(ClusterEvent.MemberStatusChange)
			);
			cluster.SendCurrentClusterState(Self);
		}

		protected override void PostStop()
		{
			if (_refreshSubscription != null)
			{
				_refreshSubscription.Dispose();
				_refreshSubscription = null;
			}
		}
	}

	public class ClusterNodeMessage
	{
		public string Name { get; set; }

		public string State { get; set; }
	}

}
