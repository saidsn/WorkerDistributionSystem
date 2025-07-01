using WorkerDistributionSystem.Application.DTOs;

namespace WorkerDistributionSystem.Application.Services.Interfaces
{
    public interface IServiceStatusService
    {
        Task<ServiceStatusDto> GetStatusAsync();
        Task StartServiceAsync();
        Task StopServiceAsync();
    }
}
