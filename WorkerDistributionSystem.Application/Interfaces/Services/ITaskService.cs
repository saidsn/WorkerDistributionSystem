using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Domain.Entities;

namespace WorkerDistributionSystem.Application.Interfaces.Services
{
    public interface ITaskService
    {
        Task<WorkerTaskDto> CreateTaskAsync(CreateTaskDto createTaskDto);
        Task<WorkerTaskDto?> GetTaskByIdAsync(Guid taskId);
        Task<List<WorkerTaskDto>> GetAllTasksAsync();
        Task<List<WorkerTaskDto>> GetTasksByWorkerIdAsync(Guid workerId);
        Task<List<WorkerTaskDto>> GetPendingTasksAsync();
        Task<bool> CompleteTaskAsync(Guid taskId, string result);
        Task<bool> FailTaskAsync(Guid taskId, string error);
        Task<int> GetQueueCountAsync();
    }
}
