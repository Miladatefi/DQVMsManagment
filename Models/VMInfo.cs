namespace DQVMsManagement.Models
{
    public class VMInfo
    {
        // Initialized to avoid "non-nullable property" warnings
        public string Name { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int    CPUUsage { get; set; }
        public long   MemoryAssignedMB { get; set; }
    }
}
