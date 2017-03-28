using Akka.Actor;
using Akka.Cluster;

namespace WampClusterMonitor
{
	public class Terminator
		: ReceiveActor
    {
		Cluster Cluster => Cluster.Get(Context.System);

		Address SelfAddress => Cluster.SelfAddress;

		public Terminator()
		{
			Receive<ClusterEvent.MemberLeft>(memberLeft =>
			{
				if (memberLeft.Member.Address == SelfAddress)
				{
					Context.System.Terminate();
				}
			});
		}

		protected override void PreStart()
		{
			base.PreStart();

			Cluster.Leave(SelfAddress);
		}
	}
}
