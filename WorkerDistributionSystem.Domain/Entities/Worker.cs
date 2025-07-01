using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Domain.Entities
{
	public class Worker
	{
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
        public WorkerStatus Status { get; set; }
        public int ProcessId { get; set; }
        public List<WorkerTask> Tasks { get; set; } = new List<WorkerTask>();
    }
}

