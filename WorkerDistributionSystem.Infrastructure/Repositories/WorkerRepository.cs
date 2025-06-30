using WorkerDistributionSystem.Application.Interfaces.Repositories;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Infrastructure.Repositories
{
    public class WorkerRepository : IWorkerRepository
    {
        private static readonly List<Worker> _workers = new();
        private static readonly object _lock = new();

        public Task<Worker> AddWorkerAsync(Worker worker)
        {
            lock (_lock)
            {
                _workers.Add(worker);
                return Task.FromResult(worker);
            }
        }

        public Task<Worker?> GetWorkerByIdAsync(Guid workerId)
        {
            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(w => w.Id == workerId);
                return Task.FromResult(worker);
            }
        }

        public Task<List<Worker>> GetAllWorkersAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(_workers.ToList());
            }
        }

        public Task<Worker> UpdateWorkerAsync(Worker worker)
        {
            lock (_lock)
            {
                var existingWorker = _workers.FirstOrDefault(w => w.Id == worker.Id);
                if (existingWorker != null)
                {
                    var index = _workers.IndexOf(existingWorker);
                    _workers[index] = worker;
                }
                return Task.FromResult(worker);
            }
        }

        public Task<bool> RemoveWorkerAsync(Guid workerId)
        {
            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(w => w.Id == workerId);
                if (worker != null)
                {
                    _workers.Remove(worker);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }

        public Task<List<Worker>> GetAvailableWorkersAsync()
        {
            lock (_lock)
            {
                var availableWorkers = _workers
                    .Where(w => w.Status == WorkerStatus.Idle)
                    .ToList();
                return Task.FromResult(availableWorkers);
            }
        }
    }
}