using System.Collections.Generic;
using System.IO;
using DQVMsManagement.Models;
using DQVMsManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace DQVMsManagement.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly UsersService   _users;
        private readonly LoggingService _logger;
        private const string HistoryPath = @"C:\loginHistory.json";

        public AdminController(UsersService users, LoggingService logger)
        {
            _users  = users;
            _logger = logger;
        }

        // GET: /Admin
        public IActionResult Index()
        {
            // Build our dashboard view model
            var vm = new AdminDashboardViewModel
            {
                Users           = _users.GetAll(),
                CurrentUserName = User.Identity?.Name ?? ""
            };

            // Load login history JSON
            if (System.IO.File.Exists(HistoryPath))
            {
                var json = System.IO.File.ReadAllText(HistoryPath);
                vm.LoginHistory = JsonConvert.DeserializeObject<List<LoginRecord>>(json)
                                  ?? new List<LoginRecord>();
            }
            else
            {
                vm.LoginHistory = new List<LoginRecord>();
            }

            return View(vm);
        }

        // GET: /Admin/Create
        public IActionResult Create() => View();

        // POST: /Admin/Create
        [HttpPost]
        public IActionResult Create(string username, string password, string role)
        {
            if (_users.Create(username, password, role, out var error))
                return RedirectToAction("Index");
            ModelState.AddModelError("", error);
            return View();
        }

        // POST: /Admin/Delete
        [HttpPost]
        public IActionResult Delete(string username)
        {
            _users.Delete(username, out var _);
            return RedirectToAction("Index");
        }

        // POST: /Admin/ToggleActive
        [HttpPost]
        public IActionResult ToggleActive(string username)
        {
            _users.ToggleActive(username, out var _);
            return RedirectToAction("Index");
        }

        // GET: /Admin/ChangePassword?username=foo
        public IActionResult ChangePassword(string username)
        {
            ViewData["Username"] = username;
            return View();
        }

        // POST: /Admin/ChangePassword
        [HttpPost]
        public IActionResult ChangePassword(string username, string newPassword)
        {
            if (_users.ChangePassword(username, newPassword, out var error))
                return RedirectToAction("Index");
            ModelState.AddModelError("", error);
            ViewData["Username"] = username;
            return View();
        }
    }
}
