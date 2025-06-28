using System;
namespace WorkerDistributionSystem.Domain.Entities
{
	public class ServiceStatus
	{
        public int ConnectedWorkers { get; set; }
        public int TotalTasksInQueue { get; set; }
        public DateTime ServiceStartTime { get; set; }
        public bool IsRunning { get; set; }
        public List<Worker> Workers { get; set; } = new List<Worker>();
    }
}

