using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Interfaces
{
	public interface ITaskRepository
	{
        Task<Guid> EnqueueTaskAsync(string command, Guid workerId);
        Task<WorkerTask?> DequeueTaskAsync(Guid workerId);
        void DequeueAllTask();
        Task UpdateTaskResultAsync(Guid taskId, string result, WorkerTaskStatus status);
        Task<List<WorkerTask>> GetWorkerTasksAsync(Guid workerId);
        Task<int> GetQueueCountAsync();
        Task<WorkerTask?> GetByIdAsync(Guid taskId);
    }
}

