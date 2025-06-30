using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Domain.Interfaces;

namespace WorkerDistributionSystem.Application.Services
{
	public class WorkerManagementService
	{
        private readonly IWorkerService _workerService;
        private readonly ITaskQueue _taskQueue;

        public WorkerManagementService(IWorkerService workerService, ITaskQueue taskQueue)
        {
            _workerService = workerService;
            _taskQueue = taskQueue;
        }

        public async Task<Guid> AddWorkerAsync(string workerName, int processId)
        {
            return await _workerService.AddAsync(workerName, processId);
        }

        public async Task<bool> RemoveWorkerAsync(Guid workerId)
        {
            return await _workerService.RemoveAsync(workerId);
        }

        public async Task<List<WorkerDto>> GetAllWorkersAsync()
        {
            var workers = await _workerService.GetAllAsync();
            var workerDtos = new List<WorkerDto>();

            foreach (var worker in workers)
            {
                var taskCount = worker.Tasks?.Count ?? 0;
                workerDtos.Add(new WorkerDto
                {
                    Id = worker.Id,
                    Name = worker.Name,
                    ConnectedAt = worker.ConnectedAt,
                    IsActive = worker.IsActive,
                    ProcessId = worker.ProcessId,
                    TaskCount = taskCount
                });
            }

            return workerDtos;
        }

        public async Task<WorkerDto?> GetWorkerStatusAsync(Guid workerId)
        {
            var worker = await _workerService.GetAsync(workerId);
            if (worker == null) return null;

            return new WorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                ConnectedAt = worker.ConnectedAt,
                IsActive = worker.IsActive,
                ProcessId = worker.ProcessId,
                TaskCount = worker.Tasks?.Count ?? 0
            };
        }
    }
}

