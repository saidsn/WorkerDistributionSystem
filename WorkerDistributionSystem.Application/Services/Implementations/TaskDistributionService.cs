using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Application.Services.Implementations
{
    public class TaskDistributionService : ITaskDistributionService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkerRepository _workerRepository;
        private readonly IServiceStatusService _serviceStatusService;

        public TaskDistributionService(
            ITaskRepository taskRepository,
            IWorkerRepository workerRepository,
            IServiceStatusService serviceStatusService)
        {
            _taskRepository = taskRepository;
            _workerRepository = workerRepository;
            _serviceStatusService = serviceStatusService;
        }

        public async Task<Guid> ExecuteCommandAsync(string command, Guid specificWorkerId)
        {
            var service = await _serviceStatusService.GetStatusAsync();
            if (!service.IsRunning)
            {
                throw new Exception($"Service is not running! Please start service");
            }

            var taskId = await _taskRepository.EnqueueTaskAsync(command, specificWorkerId);
            Console.WriteLine($"Command '{command}' queued with Task ID: {taskId}");
            return taskId;
        }

        public async Task<List<WorkerTaskDto>> GetWorkerTasksAsync(Guid workerId)
        {
            var tasks = await _taskRepository.GetWorkerTasksAsync(workerId);
            var worker = await _workerRepository.GetAsync(workerId);

            var taskDtos = new List<WorkerTaskDto>();
            foreach (var task in tasks)
            {
                taskDtos.Add(new WorkerTaskDto
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
            var task = await _taskRepository.DequeueTaskAsync(workerId);

            if (task != null)
            {
                Console.WriteLine($"Task '{task.Command}' assigned to Worker {workerId}");

                await _workerRepository.UpdateStatusAsync(workerId, WorkerStatus.Busy);
            }

            return task;
        }

        public async Task UpdateTaskResultAsync(Guid taskId, string result, WorkerTaskStatus status)
        {
            await _taskRepository.UpdateTaskResultAsync(taskId, result, status);
            Console.WriteLine($"Task {taskId} completed with status: {status}");
        }

        public async Task<int> GetQueueCountAsync()
        {
            return await _taskRepository.GetQueueCountAsync();
        }
    }
}