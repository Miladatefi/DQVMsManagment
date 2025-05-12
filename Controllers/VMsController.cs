using DQVMsManagement.Models;
using DQVMsManagement.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DQVMsManagement.Controllers
{
    public class VMsController : Controller
    {
        private readonly HyperVService _hyperV;
        public VMsController(HyperVService hyperV) => _hyperV = hyperV;

        // GET: /VMs/Index
        public IActionResult Index()
        {
            var vmList = _hyperV.GetVMs();
            return View(vmList);
        }

        // POST: /VMs/Start
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Start([FromBody] VMActionRequest req)
        {
            _hyperV.StartVM(req.Name);
            return Ok();
        }

        // POST: /VMs/Stop
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Stop([FromBody] VMActionRequest req)
        {
            _hyperV.StopVM(req.Name);
            return Ok();
        }

        // POST: /VMs/Restart
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Restart([FromBody] VMActionRequest req)
        {
            _hyperV.RestartVM(req.Name);
            return Ok();
        }

        // POST: /VMs/Checkpoint
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Checkpoint([FromBody] VMActionRequest req)
        {
            await _hyperV.CreateCheckpointAsync(req.Name);
            return Ok();
        }
    }
}
