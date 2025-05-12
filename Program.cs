using DQVMsManagement.Hubs;
using DQVMsManagement.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// 1) Add MVC services
builder.Services.AddControllersWithViews();

// 2) Register your Hyper-V management service
builder.Services.AddSingleton<HyperVService>();

// 3) Register SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// 4) Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 5) Map controllers and hub
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=VMs}/{action=Index}/{id?}");

app.MapHub<VMHub>("/vmhub");

app.Run();
