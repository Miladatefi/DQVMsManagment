// Hubs/VMHub.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DQVMsManagement.Services;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace DQVMsManagement.Hubs
{
    public class VMHub : Hub
    {
        private readonly HyperVService _hyperV;
        private readonly LoggingService _logger;
        private const string HistoryPath = @"C:\loginHistory.json";

        public VMHub(HyperVService hyperV, LoggingService logger)
        {
            _hyperV = hyperV;
            _logger = logger;
        }

        // Stream VM updates every second
        public async IAsyncEnumerable<List<Models.VMInfo>> StreamVMUpdates(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                yield return _hyperV.GetVMs();
                await Task.Delay(1000, cancellationToken);
            }
        }

        // Stream login history every 5 seconds
        public async IAsyncEnumerable<List<LoginRecord>> StreamLoginHistory(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<LoginRecord> history;
                try
                {
                    var json = File.ReadAllText(HistoryPath);
                    history = JsonConvert.DeserializeObject<List<LoginRecord>>(json)
                              ?? new List<LoginRecord>();
                }
                catch
                {
                    history = new List<LoginRecord>();
                }

                yield return history;
                await Task.Delay(5000, cancellationToken);
            }
        }

        // Create checkpoint with progress and error streaming
        public async Task CreateCheckpoint(string vmName)
        {
            await _logger.LogAsync($"Started checkpoint for VM '{vmName}'", vmName);

            string snapshotName = $"AutoCP_{DateTime.Now:yyyyMMddHHmmss}";
            using var ps = PowerShell.Create();
            ps.AddCommand("Import-Module").AddArgument("Hyper-V").Invoke();
            ps.Commands.Clear();

            ps.AddCommand("Checkpoint-VM")
              .AddParameter("VMName", vmName)
              .AddParameter("SnapshotName", snapshotName);

            ps.Streams.Progress!.DataAdded += async (sender, args) =>
            {
                var progressCollection = (PSDataCollection<ProgressRecord>)sender!;
                var record = progressCollection[args.Index];
                await Clients.Caller.SendAsync("CheckpointProgress", vmName, record.PercentComplete);
            };

            ps.Streams.Error!.DataAdded += async (sender, args) =>
            {
                var errorCollection = (PSDataCollection<ErrorRecord>)sender!;
                var errorRecord = errorCollection[args.Index];
                var msg = errorRecord.Exception?.Message ?? "Unknown error";
                await Clients.Caller.SendAsync("CheckpointError", vmName, msg);
            };

            await Task.Run(() => ps.Invoke());

            await _logger.LogAsync($"Completed checkpoint for VM '{vmName}'", vmName);

            await Clients.Caller.SendAsync("CheckpointComplete", vmName);
        }
    }
}