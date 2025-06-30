using WorkerDistributionSystem.Application.Interfaces.Services;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Service.Services
{
    public class HeartbeatMonitorService : BackgroundService
    {
        private readonly ILogger<HeartbeatMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public HeartbeatMonitorService(
            ILogger<HeartbeatMonitorService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Heartbeat Monitor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var workerService = scope.ServiceProvider.GetRequiredService<IWorkerService>();

                    await CheckInactiveWorkersAsync(workerService);

                    // Hər 60 saniyədə bir yoxla
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Heartbeat Monitor Service");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }

        private async Task CheckInactiveWorkersAsync(IWorkerService workerService)
        {
            var workers = await workerService.GetAllWorkersAsync();
            var inactiveThreshold = DateTime.UtcNow.AddMinutes(-2); // 2 dəqiqə heartbeat gəlməyibsə

            foreach (var worker in workers)
            {
                if (worker.ConnectedAt < inactiveThreshold && worker.Status != WorkerStatus.Disconnected)
                {
                    _logger.LogWarning("Worker {workerId} ({workerName}) seems inactive. Last seen: {lastSeen}",
                        worker.Id, worker.Name, worker.ConnectedAt);

                    await workerService.UpdateWorkerStatusAsync(worker.Id, false);
                }
            }
        }
    }
}