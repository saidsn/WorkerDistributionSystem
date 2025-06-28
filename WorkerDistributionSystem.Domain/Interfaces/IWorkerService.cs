using System;
using WorkerDistributionSystem.Domain.Entities;

namespace WorkerDistributionSystem.Domain.Interfaces
{
	public interface IWorkerService
	{
        Task<Guid> AddWorkerAsync(string workerName, int processId);
        Task<bool> RemoveWorkerAsync(Guid workerId);
        Task<Worker?> GetWorkerAsync(Guid workerId);
        Task<List<Worker>> GetAllWorkersAsync();
        Task<bool> UpdateWorkerStatusAsync(Guid workerId, bool isActive);
    }
}

