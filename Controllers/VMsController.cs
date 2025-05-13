using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DQVMsManagement.Models;
using DQVMsManagement.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DQVMsManagement.Controllers
{
    [Authorize]
    public class VMsController : Controller
    {
        private readonly HyperVService _hyperV;
        private readonly LoggingService _logger;

        public VMsController(HyperVService hyperV, LoggingService logger)
        {
            _hyperV = hyperV;
            _logger = logger;
        }

        // GET: /VMs/Index
        public IActionResult Index()
        {
            var vmList = _hyperV.GetVMs();
            return View(vmList);
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Start([FromBody] VMActionRequest req)
        {
            _hyperV.StartVM(req.Name);
            _logger.LogAsync($"Started VM '{req.Name}'").Wait();
            return Ok();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Stop([FromBody] VMActionRequest req)
        {
            _hyperV.StopVM(req.Name);
            _logger.LogAsync($"Stopped VM '{req.Name}'").Wait();
            return Ok();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult Restart([FromBody] VMActionRequest req)
        {
            _hyperV.RestartVM(req.Name);
            _logger.LogAsync($"Restarted VM '{req.Name}'").Wait();
            return Ok();
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Checkpoint([FromBody] VMActionRequest req)
        {
            await _hyperV.CreateCheckpointAsync(req.Name);
            await _logger.LogAsync($"Created checkpoint for VM '{req.Name}'");
            return Ok();
        }
    }
}
