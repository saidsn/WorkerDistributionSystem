using WorkerDistributionSystem.Domain.Entities;

namespace WorkerDistributionSystem.Application.Interfaces.Repositories
{
    public interface IWorkerRepository
    {
        Task<Worker> AddWorkerAsync(Worker worker);
        Task<Worker?> GetWorkerByIdAsync(Guid workerId);
        Task<List<Worker>> GetAllWorkersAsync();
        Task<Worker> UpdateWorkerAsync(Worker worker);
        Task<bool> RemoveWorkerAsync(Guid workerId);
        Task<List<Worker>> GetAvailableWorkersAsync();
    }
}
