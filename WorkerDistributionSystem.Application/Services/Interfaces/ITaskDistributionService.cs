using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Events;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Application.Services.Interfaces
{
    public interface ITaskDistributionService
    {
        Task<Guid> ExecuteCommandAsync(string command, Guid specificWorkerId);
        Task<List<WorkerTaskDto>> GetWorkerTasksAsync(Guid workerId);
        Task<WorkerTask?> GetNextTaskAsync(Guid workerId);
        Task UpdateTaskResultAsync(Guid taskId, string result, WorkerTaskStatus status);
        Task<int> GetQueueCountAsync();
    }
}
