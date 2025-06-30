using WorkerDistributionSystem.Domain.Entities;
namespace WorkerDistributionSystem.Domain.Interfaces
{
	public interface IWorkerService
	{
        Task<Guid> AddAsync(string workerName, int processId);
        Task<bool> RemoveAsync(Guid workerId);
        Task<Worker?> GetAsync(Guid workerId);
        Task<List<Worker>> GetAllAsync();
        Task<bool> UpdateStatusAsync(Guid workerId, bool isActive);
    }
}

