using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Application.Services.Implementations
{
    public class ServiceStatusService : IServiceStatusService
    {
        private readonly IServiceStatusRepository _statusRepository;

        public ServiceStatusService(
            IServiceStatusRepository statusRepository,
            IWorkerRepository workerRepository)
        {
            _statusRepository = statusRepository;
        }

        public async Task StartServiceAsync()
        {
            await _statusRepository.StartServiceAsync();
        }

        public async Task StopServiceAsync()
        {
            await _statusRepository.StopServiceAsync();
        }

        public async Task<ServiceStatusDto> GetStatusAsync()
        {
            var domainStatus = await _statusRepository.GetStatusAsync();

            var workerDtos = domainStatus.Workers.Select(w => new WorkerDto
            {
                Id = w.Id,
                Name = w.Name,
                ConnectedAt = w.ConnectedAt,
                Status = w.Status,
                ProcessId = w.ProcessId,
                TaskCount = w.Tasks?.Count ?? 0
            }).ToList();

            return new ServiceStatusDto
            {
                ConnectedWorkers = domainStatus.ConnectedWorkers,
                TotalTasksInQueue = domainStatus.TotalTasksInQueue,
                ServiceStartTime = domainStatus.ServiceStartTime,
                IsRunning = domainStatus.IsRunning,
                Workers = workerDtos
            };
        }
    }
}
