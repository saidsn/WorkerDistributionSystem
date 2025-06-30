using WorkerDistributionSystem.Application.Services;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Domain.Interfaces;

namespace WorkerDistributionSystem.WindowsService
{
	public class WorkerDistributionBackgroundService : BackgroundService
	{
        private readonly ILogger<WorkerDistributionBackgroundService> _logger;
        private readonly ICommunicationService _communicationService;
        private readonly WorkerManagementService _workerManagementService;
        private readonly TaskDistributionService _taskDistributionService;

        public WorkerDistributionBackgroundService(
            ILogger<WorkerDistributionBackgroundService> logger,
            ICommunicationService communicationService,
            WorkerManagementService workerManagementService,
            TaskDistributionService taskDistributionService)
        {
            _logger = logger;
            _communicationService = communicationService;
            _workerManagementService = workerManagementService;
            _taskDistributionService = taskDistributionService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker  Distribution Service starting...");

            try
            {
                await _communicationService.StartAsync();

                _logger.LogInformation("TCP Communication service started");

                _communicationService.MessageReceived += OnMessageReceived;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await MonitorAndDistributeTasksAsync();

                        await Task.Delay(5000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in service loop");
                        await Task.Delay(10000, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Worker Distribution Service");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Distribution Service stopping...");

            try
            {
                await _communicationService.StopAsync();
                _logger.LogInformation("TCP Communication service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping communication service");
            }

            await base.StopAsync(cancellationToken);
        }

        private async Task MonitorAndDistributeTasksAsync()
        {
            try
            {
                var workers = await _workerManagementService.GetAllWorkersAsync();
                var activeWorkers = workers.Where(w => w.IsActive).ToList();

                _logger.LogInformation($"Active workers: {activeWorkers.Count}");

                foreach (var worker in activeWorkers)
                {
                    var nextTask = await _taskDistributionService.GetNextTaskAsync(worker.Id);
                    if (nextTask != null)
                    {
                        _logger.LogInformation($"Distributing task {nextTask.Id} to worker {worker.Name}");

                        var taskMessage = $"EXECUTE:{nextTask.Command}:{nextTask.Id}";
                        await _communicationService.SendMessageAsync(worker.Id, taskMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in task distribution");
            }
        }

        private void OnMessageReceived(object? sender, string message)
        {
            try
            {
                _logger.LogInformation($"Received message: {message}");

                var parts = message.Split(':', 3);
                if (parts.Length >= 2)
                {
                    var messageType = parts[0];

                    switch (messageType)
                    {
                        case "REGISTER":
                            HandleWorkerRegistration(parts[1]);
                            break;
                        case "RESULT":
                            if (parts.Length == 3)
                            {
                                HandleTaskResult(parts[1], parts[2]);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {message}");
            }
        }

        private async void HandleWorkerRegistration(string workerName)
        {
            try
            {
                var workerId = await _workerManagementService.AddWorkerAsync(workerName, 0);
                _logger.LogInformation($"Worker {workerName} registered with ID: {workerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering worker: {workerName}");
            }
        }

        private async void HandleTaskResult(string taskIdString, string result)
        {
            try
            {
                if (Guid.TryParse(taskIdString, out var taskId))
                {
                    await _taskDistributionService.UpdateTaskResultAsync(
                        taskId,
                        result,
                        WorkerTaskStatus.Completed);

                    _logger.LogInformation($"Task {taskId} completed with result: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating task result: {taskIdString}");
            }
        }
    }
}

