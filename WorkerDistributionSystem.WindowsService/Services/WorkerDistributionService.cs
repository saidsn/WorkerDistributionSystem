using WorkerDistributionSystem.Application.Interfaces.Services;

namespace WorkerDistributionSystem.Service.Services
{
    public class WorkerDistributionService : BackgroundService
    {
        private readonly ILogger<WorkerDistributionService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WorkerDistributionService(
            ILogger<WorkerDistributionService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker Distribution Service started at: {time}", DateTimeOffset.Now);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var workerService = scope.ServiceProvider.GetRequiredService<IWorkerService>();
                    var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

                    // Service durumunu log et
                    var serviceStatus = await workerService.GetServiceStatusAsync();
                    _logger.LogInformation(
                        "Service Status - Connected Workers: {connectedWorkers}, Pending Tasks: {pendingTasks}",
                        serviceStatus.ConnectedWorkers,
                        serviceStatus.PendingTasks
                    );

                    // 30 saniyə gözlə
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker Distribution Service is stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Worker Distribution Service");
            }
            finally
            {
                _logger.LogInformation("Worker Distribution Service stopped at: {time}", DateTimeOffset.Now);
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Distribution Service is starting...");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Distribution Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}