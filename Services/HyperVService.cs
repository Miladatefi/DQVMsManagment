using System;
using System.Collections.Generic;
using System.Management.Automation;  // From Microsoft.PowerShell.SDK
using DQVMsManagement.Models;

namespace DQVMsManagement.Services
{
    public class HyperVService
    {
        public List<VMInfo> GetVMs()
        {
            var vms = new List<VMInfo>();
            using var ps = PowerShell.Create();

            // Load Hyper-V module
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();

            // Fetch all VMs
            ps.AddCommand("Get-VM");
            var results = ps.Invoke();

            foreach (var vm in results)
            {
                var nameProp  = vm.Members["Name"]?.Value?.ToString() ?? string.Empty;
                var stateProp = vm.Members["State"]?.Value?.ToString() ?? string.Empty;

                // CPU & Memory
                int cpu = vm.Members["CPUUsage"]?.Value is not null
                    ? Convert.ToInt32(vm.Members["CPUUsage"].Value)
                    : 0;
                long memMb = vm.Members["MemoryAssigned"]?.Value is not null
                    ? Convert.ToInt64(vm.Members["MemoryAssigned"].Value) / (1024 * 1024)
                    : 0L;

                // Uptime: available when VM is Running (Get-VM shows this column by default) :contentReference[oaicite:0]{index=0}
                TimeSpan? up = null;
                var upObj = vm.Members["Uptime"]?.Value;
                if (upObj is TimeSpan ts)
                    up = ts;

                vms.Add(new VMInfo
                {
                    Name             = nameProp,
                    State            = stateProp,
                    CPUUsage         = cpu,
                    MemoryAssignedMB = memMb,
                    UpTime           = up
                });
            }

            return vms;
        }

        public void StartVM(string name)
        {
            using var ps = PowerShell.Create();
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();

            ps.AddCommand("Start-VM")
              .AddParameter("Name", name)
              .Invoke();
        }

        public void StopVM(string name)
        {
            using var ps = PowerShell.Create();
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();

            ps.AddCommand("Stop-VM")
              .AddParameter("Name", name)
              .AddParameter("TurnOff", true)
              .Invoke();
        }

        public void RestartVM(string name)
        {
            using var ps = PowerShell.Create();
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();

            ps.AddCommand("Restart-VM")
              .AddParameter("Name", name)
              .AddParameter("Force", true)
              .Invoke();
        }
    }
}
