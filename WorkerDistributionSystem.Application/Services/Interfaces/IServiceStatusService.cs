using WorkerDistributionSystem.Application.DTOs;

namespace WorkerDistributionSystem.Application.Services.Interfaces
{
    public interface IServiceStatusService
    {
        Task SetServiceRunningAsync(bool isRunning);
        Task<bool> IsServiceRunningAsync();
        Task StartServiceAsync();
        Task StopServiceAsync();
        Task<ServiceStatusDto> GetStatusAsync();
    }
}
