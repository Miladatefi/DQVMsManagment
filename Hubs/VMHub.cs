using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using DQVMsManagement.Services;
using Microsoft.AspNetCore.SignalR;

namespace DQVMsManagement.Hubs
{
    public class VMHub : Hub
    {
        private readonly HyperVService _hyperV;
        public VMHub(HyperVService hyperV) => _hyperV = hyperV;

        public async IAsyncEnumerable<List<Models.VMInfo>> StreamVMUpdates(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                yield return _hyperV.GetVMs();
                await Task.Delay(1000, cancellationToken);
            }
        }

        public async Task CreateCheckpoint(string vmName)
        {
            string snapshotName = $"AutoCP_{DateTime.Now:yyyyMMddHHmmss}";

            using PowerShell ps = PowerShell.Create();
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();

            ps.AddCommand("Checkpoint-VM")
              .AddParameter("VMName", vmName)
              .AddParameter("SnapshotName", snapshotName);

            // Subscribe to progress events (null-forgiving on Streams)
            ps.Streams.Progress!.DataAdded += async (sender, args) =>
            {
                var progressCollection = (PSDataCollection<ProgressRecord>)sender!;
                var record = progressCollection[args.Index];
                await Clients.Caller.SendAsync("CheckpointProgress", vmName, record.PercentComplete);
            };

            // Subscribe to errors
            ps.Streams.Error!.DataAdded += async (sender, args) =>
            {
                var errorCollection = (PSDataCollection<ErrorRecord>)sender!;
                var errorRecord = errorCollection[args.Index];
                // safe-message, avoid dereferencing Exception if null
                var msg = errorRecord.Exception?.Message ?? "Unknown error";
                await Clients.Caller.SendAsync("CheckpointError", vmName, msg);
            };

            // Invoke (blocking; progress & errors will fire)
            await Task.Run(() => ps.Invoke());

            // Notify completion
            await Clients.Caller.SendAsync("CheckpointComplete", vmName);
        }
    }
}
