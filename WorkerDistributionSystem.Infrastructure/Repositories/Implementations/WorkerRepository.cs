using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Implementations
{
    public class WorkerRepository : IWorkerRepository
    {
        private static readonly List<Worker> _workers = new List<Worker>();
        private static readonly object _lock = new object();

        public Task<Guid> AddAsync(string workerName, int processId)
        {
            var worker = new Worker
            {
                Id = Guid.NewGuid(),
                Name = workerName,
                ProcessId = processId,
                ConnectedAt = DateTime.UtcNow,
                Status = WorkerStatus.Connected
            };

            lock (_lock)
            {
                _workers.Add(worker);
            }

            return Task.FromResult(worker.Id);
        }

        public Task<bool> RemoveAsync(Guid workerId)
        {
            bool result;

            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(w => w.Id == workerId);
                if (worker != null)
                {
                    //worker.Status = WorkerStatus.Disconnected;
                    //worker.DisconnectedAt = DateTime.UtcNow;
                    result = _workers.Remove(worker);
                }
                else
                {
                    result = false;
                }
            }

            return Task.FromResult(result);
        }

        public Task<Worker?> GetAsync(Guid workerId)
        {
            Worker? worker;

            lock (_lock)
            {
                worker = _workers.FirstOrDefault(w => w.Id == workerId);
            }

            return Task.FromResult(worker);
        }

        public Task<List<Worker>> GetAllAsync()
        {
            List<Worker> result;

            lock (_lock)
            {
                result = new List<Worker>(_workers);
            }

            return Task.FromResult(result);
        }

        public Task<bool> UpdateStatusAsync(Guid workerId, WorkerStatus status)
        {
            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(w => w.Id == workerId);
                if (worker == null)
                {
                    return Task.FromResult(false);
                }

                worker.Status = status;

                if (status == WorkerStatus.Disconnected)
                {
                    worker.DisconnectedAt = DateTime.UtcNow;
                }
            }

            return Task.FromResult(true);
        }

        public Task<Worker?> GetByNameAsync(string workerName)
        {
            var worker = _workers.FirstOrDefault(w => w.Name == workerName);
            return Task.FromResult(worker);
        }
    }
}