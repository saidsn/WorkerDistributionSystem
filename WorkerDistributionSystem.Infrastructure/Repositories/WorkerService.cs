using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Interfaces;

namespace WorkerDistributionSystem.Infrastructure.Repositories
{
	public class WorkerService : IWorkerService
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
                IsActive = true
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
                result = worker != null && _workers.Remove(worker);
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

        public Task<bool> UpdateStatusAsync(Guid workerId, bool isActive)
        {
            bool result;

            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(w => w.Id == workerId);
                if (worker != null)
                {
                    worker.IsActive = isActive;
                    result = true;
                }
                else
                {
                    result = false;
                }
            }

            return Task.FromResult(result);
        }
    }
}

