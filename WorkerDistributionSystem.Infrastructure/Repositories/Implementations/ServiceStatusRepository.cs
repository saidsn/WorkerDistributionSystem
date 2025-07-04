using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Implementations
{
    public class ServiceStatusRepository : IServiceStatusRepository
    {
        private readonly IWorkerRepository _workerRepository;

        private readonly ITaskRepository _taskRepository;

        private DateTime _serviceStartTime;

        private bool _isRunning = false;

        public ServiceStatusRepository(IWorkerRepository workerRepository, ITaskRepository taskRepository)
        {
            _workerRepository = workerRepository ?? throw new ArgumentNullException(nameof(workerRepository));
            _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
        }

        public async Task StartServiceAsync()
        {
            if (_isRunning) return;

            _isRunning = true;
            _serviceStartTime = DateTime.Now;

            await Task.CompletedTask;
        }

        public async Task StopServiceAsync()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _taskRepository.DequeueAllTask();

            await Task.CompletedTask;
        }

        public async Task<ServiceStatus> GetStatusAsync()
        {
            var workers = await _workerRepository.GetAllAsync();
            var queueCount = await _taskRepository.GetQueueCountAsync();

            return new ServiceStatus
            {
                ConnectedWorkers = workers.Count(w => w.Status == WorkerStatus.Connected),
                TotalTasksInQueue = queueCount,
                ServiceStartTime = _serviceStartTime,
                IsRunning = _isRunning,
                Workers = workers
            };
        }
    }
}
