using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Domain.Entities
{
	public class Worker
	{
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime ConnectedAt { get; set; }
        public WorkerStatus Status { get; set; }
        public int ProcessId { get; set; }
        public List<WorkerTask> Tasks { get; set; } = new List<WorkerTask>();
    }
}

