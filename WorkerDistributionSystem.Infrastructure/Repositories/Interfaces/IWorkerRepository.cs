using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;
namespace WorkerDistributionSystem.Infrastructure.Repositories.Interfaces
{
    public interface IWorkerRepository
	{
        Task<Guid> AddAsync(string workerName, int processId);
        Task<bool> RemoveAsync(Guid workerId);
        Task<Worker?> GetAsync(Guid workerId);
        Worker? GetByNameAsync(string workerName);
        Task<List<Worker>> GetAllAsync();
        Task<bool> UpdateStatusAsync(Guid workerId, WorkerStatus status);
    }
}

