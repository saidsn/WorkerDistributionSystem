using System;
namespace WorkerDistributionSystem.Domain.Entities
{
	public class Worker
	{
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime ConnectedAt { get; set; }
        public bool IsActive { get; set; }
        public int ProcessId { get; set; }
        public List<WorkerTask> Tasks { get; set; } = new List<WorkerTask>();
    }
}

