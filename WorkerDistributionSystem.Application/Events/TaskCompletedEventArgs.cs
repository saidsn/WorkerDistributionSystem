using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Application.Events
{
    public class TaskCompletedEventArgs : EventArgs
    {
        public Guid TaskId { get; set; }
        public string Result { get; set; } = string.Empty;
        public WorkerTaskStatus Status { get; set; }
    }
}
