using WorkerDistributionSystem.Domain.Entities;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Interfaces
{
    public interface IServiceStatusRepository
    {
        Task StartServiceAsync();
        Task StopServiceAsync();
        Task<ServiceStatus> GetStatusAsync();
        //Task UpdateServiceStartTimeAsync();
    }
}
