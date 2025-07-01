using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Application.DTOs
{
	public class WorkerDto
	{
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public WorkerStatus Status { get; set; }
        public int ProcessId { get; set; }
        public int TaskCount { get; set; }
    }
}

