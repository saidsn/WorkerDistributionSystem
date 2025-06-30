using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Domain.Entities
{
	public class WorkerTask
	{
        public Guid Id { get; set; }
        public string Command { get; set; }
        public Guid? WorkerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Result { get; set; }
        public WorkerTaskStatus Status { get; set; }
    }
}

