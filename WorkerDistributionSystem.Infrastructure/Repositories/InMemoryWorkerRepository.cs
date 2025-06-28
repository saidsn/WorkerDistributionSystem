using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Interfaces;

namespace WorkerDistributionSystem.Infrastructure.Repositories
{
	public class InMemoryWorkerRepository :IWorkerService
	{
        private static readonly List<Worker> _workers = new List<Worker>();
        private static readonly object _lock = new object();

        public Task<Guid> AddWorkerAsync(string workerName, int processId)
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

        public Task<bool> RemoveWorkerAsync(Guid workerId)
        {
            bool result;
            lock (_lock)
            {
                var worker = _workers.FirstOrDefault(w => w.Id == workerId);
                result = worker != null && _workers.Remove(worker);
            }

            return Task.FromResult(result);
        }

        public Task<Worker?> GetWorkerAsync(Guid workerId)
        {
            Worker? worker;
            lock (_lock)
            {
                worker = _workers.FirstOrDefault(w => w.Id == workerId);
            }

            return Task.FromResult(worker);
        }

        public Task<List<Worker>> GetAllWorkersAsync()
        {
            List<Worker> result;
            lock (_lock)
            {
                result = new List<Worker>(_workers);
            }

            return Task.FromResult(result);
        }

        public Task<bool> UpdateWorkerStatusAsync(Guid workerId, bool isActive)
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

