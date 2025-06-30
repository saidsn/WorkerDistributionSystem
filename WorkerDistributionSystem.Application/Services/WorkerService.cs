using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Exceptions;
using WorkerDistributionSystem.Application.Interfaces.Repositories;
using WorkerDistributionSystem.Application.Interfaces.Services;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Application.Services
{
    public class WorkerService : IWorkerService
    {
        private readonly IWorkerRepository _workerRepository;
        private readonly ITaskRepository _taskRepository;
        private readonly DateTime _serviceStartTime;

        public WorkerService(IWorkerRepository workerRepository, ITaskRepository taskRepository)
        {
            _workerRepository = workerRepository;
            _taskRepository = taskRepository;
            _serviceStartTime = DateTime.UtcNow;
        }

        public async Task<WorkerDto> RegisterWorkerAsync(CreateWorkerDto createWorkerDto)
        {
            var worker = new Worker
            {
                Id = Guid.NewGuid(),
                Name = createWorkerDto.Name,
                ProcessId = createWorkerDto.ProcessId,
                ConnectedAt = DateTime.UtcNow,
                Status = WorkerStatus.Idle
            };

            await _workerRepository.AddWorkerAsync(worker);

            return new WorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                ProcessId = worker.ProcessId,
                ConnectedAt = worker.ConnectedAt,
                Status = worker.Status,
                ActiveTasksCount = 0
            };
        }

        public async Task<bool> UnregisterWorkerAsync(Guid workerId)
        {
            var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
            if (worker == null)
                throw new WorkerNotFoundException(workerId);

            worker.Status = WorkerStatus.Disconnected;
            await _workerRepository.UpdateWorkerAsync(worker);

            return await _workerRepository.RemoveWorkerAsync(workerId);
        }

        public async Task<WorkerDto?> GetWorkerAsync(Guid workerId)
        {
            var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
            if (worker == null)
                return null;

            var activeTasks = await _taskRepository.GetTasksByWorkerIdAsync(worker.Id);
            var activeTasksCount = activeTasks.Count(t => t.Status == WorkerTaskStatus.InProgress);

            return new WorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                ProcessId = worker.ProcessId,
                ConnectedAt = worker.ConnectedAt,
                Status = worker.Status,
                ActiveTasksCount = activeTasksCount
            };
        }

        public async Task<List<WorkerDto>> GetAllWorkersAsync()
        {
            var workers = await _workerRepository.GetAllWorkersAsync();
            var workerDtos = new List<WorkerDto>();

            foreach (var worker in workers)
            {
                var activeTasks = await _taskRepository.GetTasksByWorkerIdAsync(worker.Id);
                var activeTasksCount = activeTasks.Count(t => t.Status == WorkerTaskStatus.InProgress);

                workerDtos.Add(new WorkerDto
                {
                    Id = worker.Id,
                    Name = worker.Name,
                    ProcessId = worker.ProcessId,
                    ConnectedAt = worker.ConnectedAt,
                    Status = worker.Status,
                    ActiveTasksCount = activeTasksCount
                });
            }

            return workerDtos;
        }

        public async Task<bool> UpdateWorkerStatusAsync(Guid workerId, bool isActive)
        {
            var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
            if (worker == null)
                return false;

            worker.Status = isActive ? WorkerStatus.Idle : WorkerStatus.Disconnected;
            worker.ConnectedAt = DateTime.UtcNow;

            await _workerRepository.UpdateWorkerAsync(worker);
            return true;
        }

        public async Task<WorkerTaskDto?> GetTaskForWorkerAsync(Guid workerId)
        {
            var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
            if (worker == null)
                throw new WorkerNotFoundException(workerId);

            if (worker.Status != WorkerStatus.Idle)
                return null;

            var pendingTasks = await _taskRepository.GetPendingTasksAsync();
            var task = pendingTasks.FirstOrDefault();

            if (task == null)
                return null;

            task.WorkerId = workerId;
            task.Status = WorkerTaskStatus.InProgress;
            worker.Status = WorkerStatus.Busy;

            await _taskRepository.UpdateTaskAsync(task);
            await _workerRepository.UpdateWorkerAsync(worker);

            return new WorkerTaskDto
            {
                Id = task.Id,
                Command = task.Command,
                WorkerId = task.WorkerId,
                WorkerName = worker.Name,
                CreatedAt = task.CreatedAt,
                Status = task.Status
            };
        }

        public async Task<bool> ProcessHeartbeatAsync(Guid workerId)
        {
            var worker = await _workerRepository.GetWorkerByIdAsync(workerId);
            if (worker == null)
                return false;

            worker.ConnectedAt = DateTime.UtcNow;

            if (worker.Status == WorkerStatus.Disconnected)
                worker.Status = WorkerStatus.Idle;

            await _workerRepository.UpdateWorkerAsync(worker);
            return true;
        }

        public async Task<ServiceStatusDto> GetServiceStatusAsync()
        {
            var workers = await _workerRepository.GetAllWorkersAsync();
            var tasks = await _taskRepository.GetTasksInQueueAsync();

            var workerDtos = new List<WorkerDto>();
            foreach (var worker in workers)
            {
                var activeTasks = await _taskRepository.GetTasksByWorkerIdAsync(worker.Id);
                var activeTasksCount = activeTasks.Count(t => t.Status == WorkerTaskStatus.InProgress);

                workerDtos.Add(new WorkerDto
                {
                    Id = worker.Id,
                    Name = worker.Name,
                    ProcessId = worker.ProcessId,
                    ConnectedAt = worker.ConnectedAt,
                    Status = worker.Status,
                    ActiveTasksCount = activeTasksCount
                });
            }

            return new ServiceStatusDto
            {
                ConnectedWorkers = workers.Count(w => w.Status != WorkerStatus.Disconnected),
                BusyWorkers = workers.Count(w => w.Status == WorkerStatus.Busy),
                IdleWorkers = workers.Count(w => w.Status == WorkerStatus.Idle),
                PendingTasks = tasks.Count(t => t.Status == WorkerTaskStatus.Pending),
                CompletedTasks = tasks.Count(t => t.Status == WorkerTaskStatus.Completed),
                FailedTasks = tasks.Count(t => t.Status == WorkerTaskStatus.Failed),
                ServiceStartTime = _serviceStartTime,
                IsRunning = true,
                Workers = workerDtos
            };
        }
    }
}