using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusterDemo.Client.Controllers
{
    using Actors.Client;

    [Route("api/v1/jobs")]
    public class JobsController
        : Controller
    {
        readonly ClientApp _clientApp;

        public JobsController(ClientApp clientApp)
        {
            _clientApp = clientApp;
        }

        [HttpPost]
        public IActionResult CreateJobs([FromBody] string[] jobNames)
        {
            foreach (string jobName in jobNames)
                _clientApp.SubmitJob(jobName);

            return Ok(new
            {
                Result = "Success",
                Message = $"{jobNames.Length} jobs submitted."
            });
        }
    }
}
