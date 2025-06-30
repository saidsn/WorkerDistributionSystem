using WorkerDistributionSystem.Domain.Entities;

namespace WorkerDistributionSystem.Application.Interfaces.Repositories
{
    public interface ITaskRepository
    {
        Task<WorkerTask> AddTaskAsync(WorkerTask task);
        Task<WorkerTask?> GetTaskByIdAsync(Guid taskId);
        Task<List<WorkerTask>> GetTasksByWorkerIdAsync(Guid workerId);
        Task<List<WorkerTask>> GetPendingTasksAsync();
        Task<WorkerTask> UpdateTaskAsync(WorkerTask task);
        Task<List<WorkerTask>> GetTasksInQueueAsync();
    }
}
