using System;
using System.Linq;
using System.Security.Claims;
using DQVMsManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DQVMsManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly UsersService _users;
        private readonly LoggingService _logger;

        public AccountController(UsersService users, LoggingService logger)
        {
            _users  = users;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");
                return RedirectToAction("Index", "VMs");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password, string? returnUrl)
        {
            if (_users.Validate(username, password, out var role))
            {
                // Generate & persist session token
                var newSessionId = Guid.NewGuid().ToString();
                _users.SetSessionId(username, newSessionId);

                // Build claims
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("SessionId", newSessionId)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Sign in (session cookie)
                HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = false, AllowRefresh = false }
                ).Wait();

                // Extract IP
                var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0]
                         ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                         ?? "unknown-ip";

                // **Pass overrideUser so it’s not “anonymous”**
                _logger.LogAsync($"User '{username}' logged in from {ip}", username).Wait();

                // Redirect based on role or returnUrl
                if (role == "Admin")
                    return RedirectToAction("Index", "Admin");
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "VMs");
            }

            ModelState.AddModelError(string.Empty, "Invalid username or password");
            return View();
        }

        [HttpPost]
        public IActionResult Logout()
        {
            var user = User.Identity?.Name ?? "anonymous";
            var ip   = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0]
                       ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                       ?? "unknown-ip";

            // **Again pass overrideUser explicitly**
            _logger.LogAsync($"User '{user}' logged out from {ip}", user).Wait();

            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewData["Username"] = User.Identity?.Name;
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult ChangePassword(string newPassword)
        {
            var currentUser = User.Identity?.Name ?? string.Empty;
            if (_users.ChangePassword(currentUser, newPassword, out var error))
            {
                _logger.LogAsync($"User '{currentUser}' changed their own password", currentUser).Wait();
                return RedirectToAction("Index", "VMs");
            }
            ModelState.AddModelError(string.Empty, error);
            ViewData["Username"] = currentUser;
            return View();
        }
    }
}
