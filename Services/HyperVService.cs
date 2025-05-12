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

            // 1) Ensure Hyper-V module is loaded
            ps.AddCommand("Import-Module").AddArgument("Hyper-V");
            ps.Invoke();
            ps.Commands.Clear();

            // 2) Get all VMs on local host
            ps.AddCommand("Get-VM");
            var results = ps.Invoke();

            // 3) Map each PSObject to VMInfo
            foreach (var vm in results)
            {
                var nameProp  = vm.Members["Name"]?.Value?.ToString() ?? string.Empty;
                var stateProp = vm.Members["State"]?.Value?.ToString() ?? string.Empty;

                int cpu = vm.Members["CPUUsage"]?.Value is not null
                    ? Convert.ToInt32(vm.Members["CPUUsage"].Value)
                    : 0;

                long memMb = vm.Members["MemoryAssigned"]?.Value is not null
                    ? Convert.ToInt64(vm.Members["MemoryAssigned"].Value) / (1024 * 1024)
                    : 0L;

                vms.Add(new VMInfo
                {
                    Name             = nameProp,
                    State            = stateProp,
                    CPUUsage         = cpu,
                    MemoryAssignedMB = memMb
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
