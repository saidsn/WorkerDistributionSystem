namespace WorkerDistributionSystem.Application.DTOs
{
    public class ServiceStatusDto
    {
        public int ConnectedWorkers { get; set; }
        public int BusyWorkers { get; set; }
        public int IdleWorkers { get; set; }
        public int PendingTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int FailedTasks { get; set; }
        public DateTime ServiceStartTime { get; set; }
        public bool IsRunning { get; set; } = true;
        public List<WorkerDto> Workers { get; set; } = new List<WorkerDto>(); 
    }
}