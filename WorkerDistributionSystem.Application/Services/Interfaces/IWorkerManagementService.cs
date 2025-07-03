using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Application.Services.Interfaces
{
    public interface IWorkerManagementService
    {
        Task<Guid> AddWorkerAsync(string workerName, int processId);
        Task<bool> RemoveWorkerAsync(Guid workerId);
        Task<List<WorkerDto>> GetAllWorkersAsync();
        Task<WorkerDto?> GetWorkerStatusAsync(Guid workerId);
        Task<WorkerDto?> GetWorkerByNameAsync(string workerName);
        Task UpdateStatusAsync(Guid workerId, WorkerStatus status);
    }
}