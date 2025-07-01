using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Implementations
{
	public class TaskRepository : ITaskRepository
	{
        private static readonly Queue<WorkerTask> _taskQueue = new Queue<WorkerTask>();
        private static readonly object _lock = new object();

        public Task<Guid> EnqueueTaskAsync(string command, Guid workerId)
        {
            var task = new WorkerTask
            {
                Id = Guid.NewGuid(),
                Command = command,
                WorkerId = workerId,
                CreatedAt = DateTime.UtcNow,
                Status = WorkerTaskStatus.Pending
            };

            lock (_lock)
            {
                _taskQueue.Enqueue(task);
            }

            return Task.FromResult(task.Id);
        }

        public Task<WorkerTask?> DequeueTaskAsync(Guid workerId)
        {
            WorkerTask? task = null;

            lock (_lock)
            {
                if (_taskQueue.Count > 0)
                {
                    task = _taskQueue.Dequeue();
                    task.WorkerId = workerId;
                    task.Status = WorkerTaskStatus.InProgress;
                }
            }

            return Task.FromResult(task);
        }

        public void DequeueAllTask()
        {
            _taskQueue.Clear();
        }

        public Task UpdateTaskResultAsync(Guid taskId, string result, WorkerTaskStatus status)
        {
            lock (_lock)
            {
                var task = _taskQueue.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    task.Result = result;
                    task.Status = status;
                    task.CompletedAt = DateTime.UtcNow;
                }
            }

            return Task.CompletedTask;
        }

        public Task<List<WorkerTask>> GetWorkerTasksAsync(Guid workerId)
        {
            List<WorkerTask> tasks;

            lock (_lock)
            {
                tasks = _taskQueue.Where(t => t.WorkerId == workerId).ToList();
            }

            return Task.FromResult(tasks);
        }

        public Task<int> GetQueueCountAsync()
        {
            int count;

            lock (_lock)
            {
                count = _taskQueue.Count;
            }

            return Task.FromResult(count);
        }
    }
}

