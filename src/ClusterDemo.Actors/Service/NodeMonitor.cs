using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    using Common;

    public class NodeMonitor
        : ReceiveActorEx
    {
        public NodeMonitor()
        {
            // TODO: Use WAMP client to publish NodeStats.
        }
    }
}
