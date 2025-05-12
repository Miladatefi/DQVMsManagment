using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using DQVMsManagement.Models;

namespace DQVMsManagement.Services
{
    public class HyperVService
    {
        public List<VMInfo> GetVMs()
        {
            var vms = new List<VMInfo>();

            using var ps = PowerShell.Create();
            // 1) Load Hyper-V module
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();

            // 2) Now actually call Get-VM
            ps.AddCommand("Get-VM");
            Collection<PSObject> results = ps.Invoke() ?? new Collection<PSObject>();

            foreach (var vm in results)
            {
                try
                {
                    // Basic properties
                    string name  = vm.Members["Name"]?.Value?.ToString() ?? "<unknown>";
                    string state = vm.Members["State"]?.Value?.ToString() ?? "<unknown>";

                    int cpu = 0;
                    if (vm.Members["CPUUsage"]?.Value is object cpuVal)
                        cpu = Convert.ToInt32(cpuVal);

                    long memMb = 0;
                    if (vm.Members["MemoryAssigned"]?.Value is object memVal)
                        memMb = Convert.ToInt64(memVal) / (1024 * 1024);

                    // Uptime
                    TimeSpan upTs = TimeSpan.Zero;
                    string upTimeStr = "–";
                    if (vm.Members["Uptime"]?.Value is TimeSpan ts)
                    {
                        upTs = ts;
                        upTimeStr = ts.ToString(@"dd\:hh\:mm\:ss");
                    }

                    // Last checkpoint
                    DateTime? lastCp = null;
                    string lastCpStr = string.Empty;
                    try
                    {
                        using var cps = PowerShell.Create();
                        cps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
                        cps.Commands.Clear();

                        cps.AddScript($@"
                            (Get-VMSnapshot -VMName '{name}' |
                             Sort-Object CreationTime -Descending |
                             Select-Object -First 1).CreationTime
                        ");

                        Collection<PSObject> cpResult = cps.Invoke() ?? new Collection<PSObject>();
                        if (cpResult.Count > 0 && cpResult[0].BaseObject is DateTime dt)
                        {
                            lastCp = dt;
                            lastCpStr = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                    }
                    catch
                    {
                        // ignore checkpoint lookup errors
                    }

                    // Can manage if ≤2 min up OR checkpoint in last 2 min
                    bool canManage = upTs <= TimeSpan.FromMinutes(2)
                                     || (lastCp.HasValue && (DateTime.Now - lastCp.Value) <= TimeSpan.FromMinutes(2));

                    vms.Add(new VMInfo
                    {
                        Name               = name,
                        State              = state,
                        CPUUsage           = cpu,
                        MemoryAssignedMB   = memMb,
                        UpTime             = upTimeStr,
                        LastCheckpointTime = lastCpStr,
                        CanManage          = canManage
                    });
                }
                catch
                {
                    // Skip any VM that throws
                }
            }

            return vms;
        }

        public void StartVM(string name)
        {
            using var ps = PowerShell.Create();
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();
            ps.AddCommand("Start-VM").AddParameter("Name", name).Invoke();
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

        public async Task CreateCheckpointAsync(string name)
        {
            await Task.Run(() =>
            {
                using var ps = PowerShell.Create();
                ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
                ps.Commands.Clear();
                var snapshotName = $"AutoCheckpoint_{DateTime.Now:yyyyMMddHHmmss}";
                ps.AddCommand("Checkpoint-VM")
                  .AddParameter("VMName", name)
                  .AddParameter("SnapshotName", snapshotName)
                  .Invoke();
            });
        }
    }
}
