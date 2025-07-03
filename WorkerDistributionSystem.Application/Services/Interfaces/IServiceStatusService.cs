using WorkerDistributionSystem.Application.DTOs;

namespace WorkerDistributionSystem.Application.Services.Interfaces
{
    public interface IServiceStatusService
    {
        Task StartServiceAsync();
        Task StopServiceAsync();
        Task<ServiceStatusDto> GetStatusAsync();
    }
}
