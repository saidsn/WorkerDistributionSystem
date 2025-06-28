using WorkerDistributionSystem.Domain.Entities;

namespace WorkerDistributionSystem.Domain.Interfaces
{
	public interface ITaskQueue
	{
        Task<Guid> EnqueueTaskAsync(string command, Guid? workerId = null);
        Task<WorkerTask?> DequeueTaskAsync(Guid workerId);
        Task UpdateTaskResultAsync(Guid taskId, string result, WorkerTaskStatus status);
        Task<List<WorkerTask>> GetWorkerTasksAsync(Guid workerId);
        Task<int> GetQueueCountAsync();
    }
}

