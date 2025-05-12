using Microsoft.AspNetCore.Mvc;
using DQVMsManagement.Models;
using DQVMsManagement.Services;
using System.Collections.Generic;

namespace DQVMsManagement.Controllers
{
    public class VMsController : Controller
    {
        private readonly HyperVService _hyperV;

        public VMsController(HyperVService hyperV)
        {
            _hyperV = hyperV;
        }

        // GET: /VMs/Index
        public IActionResult Index()
        {
            List<VMInfo> vmList = _hyperV.GetVMs();
            return View(vmList);
        }

        // POST: /VMs/Start
        [HttpPost]
        public IActionResult Start(string name)
        {
            _hyperV.StartVM(name);
            return RedirectToAction(nameof(Index));
        }

        // POST: /VMs/Stop
        [HttpPost]
        public IActionResult Stop(string name)
        {
            _hyperV.StopVM(name);
            return RedirectToAction(nameof(Index));
        }

        // POST: /VMs/Restart
        [HttpPost]
        public IActionResult Restart(string name)
        {
            _hyperV.RestartVM(name);
            return RedirectToAction(nameof(Index));
        }
    }
}
