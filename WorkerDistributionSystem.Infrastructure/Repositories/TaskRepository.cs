using WorkerDistributionSystem.Application.Interfaces.Repositories;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Infrastructure.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private static readonly List<WorkerTask> _tasks = new();
        private static readonly object _lock = new();

        public Task<WorkerTask> AddTaskAsync(WorkerTask task)
        {
            lock (_lock)
            {
                _tasks.Add(task);
                return Task.FromResult(task);
            }
        }

        public Task<WorkerTask?> GetTaskByIdAsync(Guid taskId)
        {
            lock (_lock)
            {
                var task = _tasks.FirstOrDefault(t => t.Id == taskId);
                return Task.FromResult(task);
            }
        }

        public Task<List<WorkerTask>> GetTasksByWorkerIdAsync(Guid workerId)
        {
            lock (_lock)
            {
                var workerTasks = _tasks
                    .Where(t => t.WorkerId == workerId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToList();
                return Task.FromResult(workerTasks);
            }
        }

        public Task<List<WorkerTask>> GetPendingTasksAsync()
        {
            lock (_lock)
            {
                var pendingTasks = _tasks
                    .Where(t => t.Status == WorkerTaskStatus.Pending)
                    .OrderBy(t => t.CreatedAt)
                    .ToList();
                return Task.FromResult(pendingTasks);
            }
        }

        public Task<WorkerTask> UpdateTaskAsync(WorkerTask task)
        {
            lock (_lock)
            {
                var existingTask = _tasks.FirstOrDefault(t => t.Id == task.Id);
                if (existingTask != null)
                {
                    var index = _tasks.IndexOf(existingTask);
                    _tasks[index] = task;
                }
                return Task.FromResult(task);
            }
        }

        public Task<List<WorkerTask>> GetTasksInQueueAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_tasks.OrderByDescending(t => t.CreatedAt).ToList());
            }
        }
    }
}