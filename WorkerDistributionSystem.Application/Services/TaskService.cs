using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Exceptions;
using WorkerDistributionSystem.Application.Interfaces.Repositories;
using WorkerDistributionSystem.Application.Interfaces.Services;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IWorkerRepository _workerRepository;

        public TaskService(ITaskRepository taskRepository, IWorkerRepository workerRepository)
        {
            _taskRepository = taskRepository;
            _workerRepository = workerRepository;
        }

        public async Task<WorkerTaskDto> CreateTaskAsync(CreateTaskDto createTaskDto)
        {
            var task = new WorkerTask
            {
                Id = Guid.NewGuid(),
                Command = createTaskDto.Command,
                CreatedAt = DateTime.UtcNow,
                Status = WorkerTaskStatus.Pending
            };

            await _taskRepository.AddTaskAsync(task);

            return new WorkerTaskDto
            {
                Id = task.Id,
                Command = task.Command,
                CreatedAt = task.CreatedAt,
                Status = task.Status
            };
        }

        public async Task<WorkerTaskDto?> GetTaskByIdAsync(Guid taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null)
                return null;

            string? workerName = null;
            if (task.WorkerId.HasValue)
            {
                var worker = await _workerRepository.GetWorkerByIdAsync(task.WorkerId.Value);
                workerName = worker?.Name;
            }

            return new WorkerTaskDto
            {
                Id = task.Id,
                Command = task.Command,
                WorkerId = task.WorkerId,
                WorkerName = workerName,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                Result = task.Result,
                Status = task.Status
            };
        }

        public async Task<List<WorkerTaskDto>> GetAllTasksAsync()
        {
            var tasks = await _taskRepository.GetTasksInQueueAsync();
            var taskDtos = new List<WorkerTaskDto>();

            foreach (var task in tasks)
            {
                string? workerName = null;
                if (task.WorkerId.HasValue)
                {
                    var worker = await _workerRepository.GetWorkerByIdAsync(task.WorkerId.Value);
                    workerName = worker?.Name;
                }

                taskDtos.Add(new WorkerTaskDto
                {
                    Id = task.Id,
                    Command = task.Command,
                    WorkerId = task.WorkerId,
                    WorkerName = workerName,
                    CreatedAt = task.CreatedAt,
                    CompletedAt = task.CompletedAt,
                    Result = task.Result,
                    Status = task.Status
                });
            }

            return taskDtos;
        }

        public async Task<List<WorkerTaskDto>> GetTasksByWorkerIdAsync(Guid workerId)
        {
            var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
            if (worker == null)
                throw new WorkerNotFoundException(workerId);

            var tasks = await _taskRepository.GetTasksByWorkerIdAsync(workerId);

            return tasks.Select(task => new WorkerTaskDto
            {
                Id = task.Id,
                Command = task.Command,
                WorkerId = task.WorkerId,
                WorkerName = worker.Name,
                CreatedAt = task.CreatedAt,
                CompletedAt = task.CompletedAt,
                Result = task.Result,
                Status = task.Status
            }).ToList();
        }

        public async Task<List<WorkerTaskDto>> GetPendingTasksAsync()
        {
            var tasks = await _taskRepository.GetPendingTasksAsync();

            return tasks.Select(task => new WorkerTaskDto
            {
                Id = task.Id,
                Command = task.Command,
                CreatedAt = task.CreatedAt,
                Status = task.Status
            }).ToList();
        }

        public async Task<bool> CompleteTaskAsync(Guid taskId, string result)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null)
                throw new TaskNotFoundException(taskId);

            task.Status = WorkerTaskStatus.Completed;
            task.Result = result;
            task.CompletedAt = DateTime.UtcNow;

            if (task.WorkerId.HasValue)
            {
                var worker = await _workerRepository.GetWorkerByIdAsync(task.WorkerId.Value);
                if (worker != null)
                {
                    worker.Status = WorkerStatus.Idle;
                    await _workerRepository.UpdateWorkerAsync(worker);
                }
            }

            await _taskRepository.UpdateTaskAsync(task);
            return true;
        }

        public async Task<bool> FailTaskAsync(Guid taskId, string error)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null)
                throw new TaskNotFoundException(taskId);

            task.Status = WorkerTaskStatus.Failed;
            task.Result = error;
            task.CompletedAt = DateTime.UtcNow;

            if (task.WorkerId.HasValue)
            {
                var worker = await _workerRepository.GetWorkerByIdAsync(task.WorkerId.Value);
                if (worker != null)
                {
                    worker.Status = WorkerStatus.Idle;
                    await _workerRepository.UpdateWorkerAsync(worker);
                }
            }

            await _taskRepository.UpdateTaskAsync(task);
            return true;
        }

        public async Task<int> GetQueueCountAsync()
        {
            var pendingTasks = await _taskRepository.GetPendingTasksAsync();
            return pendingTasks.Count;
        }
    }
}