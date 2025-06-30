using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Application.DTOs
{
	public class WorkerTaskDto
	{
        public Guid Id { get; set; }
        public string Command { get; set; }
        public Guid? WorkerId { get; set; }
        public string? WorkerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Result { get; set; }
        public WorkerTaskStatus Status { get; set; }
    }
}

