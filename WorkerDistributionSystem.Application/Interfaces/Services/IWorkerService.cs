using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Domain.Entities;

namespace WorkerDistributionSystem.Application.Interfaces.Services
{
    public interface IWorkerService
    {
        Task<WorkerDto> RegisterWorkerAsync(CreateWorkerDto createWorkerDto);
        Task<bool> UnregisterWorkerAsync(Guid workerId);
        Task<WorkerDto?> GetWorkerAsync(Guid workerId);
        Task<List<WorkerDto>> GetAllWorkersAsync();
        Task<bool> UpdateWorkerStatusAsync(Guid workerId, bool isActive);
        Task<WorkerTaskDto?> GetTaskForWorkerAsync(Guid workerId);
        Task<bool> ProcessHeartbeatAsync(Guid workerId);
        Task<ServiceStatusDto> GetServiceStatusAsync();
    }
}
