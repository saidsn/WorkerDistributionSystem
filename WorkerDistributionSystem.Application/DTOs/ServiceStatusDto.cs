namespace WorkerDistributionSystem.Application.DTOs
{
	public class ServiceStatusDto
	{
        public int ConnectedWorkers { get; set; }
        public int TotalTasksInQueue { get; set; }
        public DateTime ServiceStartTime { get; set; }
        public bool IsRunning { get; set; }
        public List<WorkerDto> Workers { get; set; } = new List<WorkerDto>();
    }
}

