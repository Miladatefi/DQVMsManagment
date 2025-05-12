using System;

namespace DQVMsManagement.Models
{
    public class VMInfo
    {
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int    CPUUsage { get; set; }
        public long   MemoryAssignedMB { get; set; }

        public TimeSpan? UpTime { get; set; }
}
}