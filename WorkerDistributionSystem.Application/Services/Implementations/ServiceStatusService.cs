using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

public class ServiceStatusService : IServiceStatusService
{
    private readonly IServiceStatusRepository _statusRepository;

    public ServiceStatusService(IServiceStatusRepository statusRepository)
    {
        _statusRepository = statusRepository ?? throw new ArgumentNullException(nameof(statusRepository));
    }

    public async Task SetServiceRunningAsync(bool isRunning)
    {
        if (isRunning)
        {
            await _statusRepository.StartServiceAsync();
        }
        else
        {
            await _statusRepository.StopServiceAsync();
        }
    }

    public async Task<bool> IsServiceRunningAsync()
    {
        var status = await _statusRepository.GetStatusAsync();
        return status.IsRunning;
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
