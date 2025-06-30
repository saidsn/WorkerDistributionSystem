using WorkerDistributionSystem.Application.Interfaces.Services;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.Service.Services
{
    public class HeartbeatMonitorService : BackgroundService
    {
        private readonly ILogger<HeartbeatMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromMinutes(2); // 2 dəqiqə timeout

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

            _logger.LogInformation("Heartbeat Monitor Service stopped");
        }

        private async Task CheckInactiveWorkersAsync(IWorkerService workerService)
        {
            var workers = await workerService.GetAllWorkersAsync();
            var inactiveThreshold = DateTime.UtcNow.Subtract(_heartbeatTimeout);
            int inactiveCount = 0;

            foreach (var worker in workers)
            {
                if (worker.ConnectedAt < inactiveThreshold && worker.Status != WorkerStatus.Disconnected)
                {
                    _logger.LogWarning(
                        "Worker {workerId} ({workerName}) marked as inactive. Last heartbeat: {lastSeen}",
                        worker.Id,
                        worker.Name,
                        worker.ConnectedAt
                    );

                    await workerService.UpdateWorkerStatusAsync(worker.Id, false);
                    inactiveCount++;
                }
            }

            if (inactiveCount > 0)
            {
                _logger.LogInformation("Marked {count} workers as inactive", inactiveCount);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Heartbeat Monitor Service is starting...");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Heartbeat Monitor Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}