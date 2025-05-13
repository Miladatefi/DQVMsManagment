using DQVMsManagement.Hubs;
using DQVMsManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Bind to LAN interface (or 0.0.0.0 for all interfaces)
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// MVC & SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Core services
builder.Services.AddSingleton<HyperVService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<LoggingService>();
builder.Services.AddSingleton<UsersService>();

// Authentication & single-session enforcement
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.HttpOnly  = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite     = SameSiteMode.Strict;
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var userName     = ctx.Principal?.Identity?.Name;
            var claimSession = ctx.Principal?.FindFirst("SessionId")?.Value;
            if (!string.IsNullOrEmpty(userName))
            {
                var usersSvc = ctx.HttpContext.RequestServices.GetRequiredService<UsersService>();
                var validSession = usersSvc.GetSessionId(userName);
                if (claimSession == null || claimSession != validSession)
                {
                    ctx.RejectPrincipal();
                    await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

var app = builder.Build();

// Error handling & HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Forwarded headers for real client IPs
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies  = { }
});

app.UseAuthentication();
app.UseAuthorization();

// Routes & hubs
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");
app.MapHub<VMHub>("/vmhub");

app.Run();
