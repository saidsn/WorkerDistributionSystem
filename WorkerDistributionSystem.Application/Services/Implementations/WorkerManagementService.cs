using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Application.Services.Implementations
{
    public class WorkerManagementService : IWorkerManagementService
    {
        private readonly IWorkerRepository _workerRepository;

        public WorkerManagementService(IWorkerRepository workerRepository)
        {
            _workerRepository = workerRepository;
        }

        public async Task<Guid> AddWorkerAsync(string workerName, int processId)
        {
            var workerId = await _workerRepository.AddAsync(workerName, processId);
            return workerId;
        }

        public async Task<bool> RemoveWorkerAsync(Guid workerId)
        {
            var result = await _workerRepository.RemoveAsync(workerId);
            return result;
        }

        public async Task<List<WorkerDto>> GetAllWorkersAsync()
        {
            var workers = await _workerRepository.GetAllAsync();
            var workerDtos = new List<WorkerDto>();

            foreach (var worker in workers)
            {
                var taskCount = worker.Tasks?.Count ?? 0;
                workerDtos.Add(new WorkerDto
                {
                    Id = worker.Id,
                    Name = worker.Name,
                    ConnectedAt = worker.ConnectedAt,
                    Status = worker.Status,
                    ProcessId = worker.ProcessId,
                    TaskCount = taskCount
                });
            }

            return workerDtos;
        }

        public async Task<WorkerDto?> GetWorkerStatusAsync(Guid workerId)
        {
            var worker = await _workerRepository.GetAsync(workerId);
            if (worker == null) return null;

            return new WorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                ConnectedAt = worker.ConnectedAt,
                Status = worker.Status,
                ProcessId = worker.ProcessId,
                TaskCount = worker.Tasks?.Count ?? 0
            };
        }

        public async Task<WorkerDto?> GetWorkerByNameAsync(string workerName)
        {
            var worker = _workerRepository.GetByNameAsync(workerName);
            if (worker == null)
            {
                return null;
            }

            var dto = new WorkerDto
            {
                Id = worker.Id,
                Name = worker.Name,
                ConnectedAt = worker.ConnectedAt,
                Status = worker.Status,
                ProcessId = worker.ProcessId,
                TaskCount = worker.Tasks?.Count ?? 0
            };

            return dto;
        }
    }
}