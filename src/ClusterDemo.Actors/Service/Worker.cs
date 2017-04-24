using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service
{
    public class Worker
        : ReceiveActorEx
    {
        readonly int _id;
        readonly IActorRef _dispatcher;

        public Worker(int id, IActorRef dispatcher)
        {
            _id = id;
            _dispatcher = dispatcher;
        }
    }
}
