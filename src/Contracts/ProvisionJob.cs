using System;

namespace Contracts
{
    public class ProvisionJob
    {
        public string JobId { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; }
    }

}
