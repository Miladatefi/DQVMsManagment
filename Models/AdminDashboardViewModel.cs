using System.Collections.Generic;
using DQVMsManagement.Services;

namespace DQVMsManagement.Models
{
    public class AdminDashboardViewModel
    {
        public IEnumerable<UserRecord> Users        { get; set; } = null!;
        public IEnumerable<LoginRecord> LoginHistory { get; set; } = null!;
        public string CurrentUserName                { get; set; } = "";
    }
}
