using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Actors.Service.Messages
{
    public class JobCompleted
    {
        public JobCompleted(int id, params string[] messages)
            : this(id, (IEnumerable<string>)messages)
        {
        }

        public JobCompleted(int id, IEnumerable<string> messages)
        {
            Id = id;
            messages = ImmutableList.CreateRange(messages);
        }

        public int Id { get; }
        public ImmutableList<string> Messages;
    }
}
