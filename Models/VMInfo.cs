namespace DQVMsManagement.Models
{
    public class VMInfo
    {
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int    CPUUsage { get; set; }
        public long   MemoryAssignedMB { get; set; }

        /// <summary>
        /// Formatted uptime (dd:hh:mm:ss) or “–” if not running.
        /// </summary>
        public string UpTime { get; set; } = "–";

        /// <summary>
        /// Last checkpoint creation time (yyyy-MM-dd HH:mm:ss) or empty.
        /// </summary>
        public string LastCheckpointTime { get; set; } = string.Empty;

        /// <summary>
        /// True if VM has been up ≤2 min or has a checkpoint in the last 2 minutes.
        /// </summary>
        public bool CanManage { get; set; }
    }
}
