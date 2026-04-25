using System;

namespace SunloginManager.Models
{
    public class ConnectionHistory
    {
        public int Id { get; set; }
        public int ConnectionId { get; set; }
        public string ConnectionName { get; set; } = string.Empty;
        public string IdentificationCode { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public ConnectionStatus Status { get; set; } = ConnectionStatus.Success;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public enum ConnectionStatus
    {
        Success,
        Failed,
        ClientNotFound,
        InvalidCode
    }
}
