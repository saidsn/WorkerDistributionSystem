using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Interfaces;

namespace WorkerDistributionSystem.Application.Services
{
	public class TaskDistributionService
	{
        private readonly ITaskQueue _taskQueue;
        private readonly IWorkerService _workerService;

        public TaskDistributionService(ITaskQueue taskQueue, IWorkerService workerService)
        {
            _taskQueue = taskQueue;
            _workerService = workerService;
        }

        public async Task<Guid> ExecuteCommandAsync(string command, Guid? specificWorkerId = null)
        {
            return await _taskQueue.EnqueueTaskAsync(command, specificWorkerId);
        }

        public async Task<List<TaskDto>> GetWorkerTasksAsync(Guid workerId)
        {
            var tasks = await _taskQueue.GetWorkerTasksAsync(workerId);
            var worker = await _workerService.GetWorkerAsync(workerId);

            var taskDtos = new List<TaskDto>();
            foreach (var task in tasks)
            {
                taskDtos.Add(new TaskDto
                {
                    Id = task.Id,
                    Command = task.Command,
                    WorkerId = task.WorkerId,
                    WorkerName = worker?.Name ?? "Unknown",
                    CreatedAt = task.CreatedAt,
                    CompletedAt = task.CompletedAt,
                    Result = task.Result,
                    Status = task.Status
                });
            }

            return taskDtos;
        }

        public async Task<WorkerTask?> GetNextTaskAsync(Guid workerId)
        {
            return await _taskQueue.DequeueTaskAsync(workerId);
        }

        public async Task UpdateTaskResultAsync(Guid taskId, string result, WorkerTaskStatus status)
        {
            await _taskQueue.UpdateTaskResultAsync(taskId, result, status);
        }

        public async Task<int> GetQueueCountAsync()
        {
            return await _taskQueue.GetQueueCountAsync();
        }
    }
}

