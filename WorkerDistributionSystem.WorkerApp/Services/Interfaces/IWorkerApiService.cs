using WorkerDistributionSystem.Application.DTOs;

namespace WorkerDistributionSystem.WorkerApp.Services.Interfaces
{
    public interface IWorkerApiService
    {
        Task<WorkerDto?> RegisterWorkerAsync(string workerName);
        Task<bool> SendHeartbeatAsync(Guid workerId);
        Task<WorkerTaskDto?> GetTaskAsync(Guid workerId);
        Task<bool> CompleteTaskAsync(Guid taskId, string result);
        Task<bool> FailTaskAsync(Guid taskId, string error);
        Task<bool> UnregisterWorkerAsync(Guid workerId);
    }
}