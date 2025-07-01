using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.WindowsService
{
    public class WorkerDistributionBackgroundService : BackgroundService
    {
        private readonly ILogger<WorkerDistributionBackgroundService> _logger;
        private readonly ICommunicationRepository _communicationRepository;
        private readonly IWorkerManagementService _workerManagementService;
        private readonly ITaskDistributionService _taskDistributionService;
        private readonly Dictionary<string, Guid> _workerConnections = new();

        public WorkerDistributionBackgroundService(
            ILogger<WorkerDistributionBackgroundService> logger,
            ICommunicationRepository communicationRepository,
            IWorkerManagementService workerManagementService,
            ITaskDistributionService taskDistributionService)
        {
            _logger = logger;
            _communicationRepository = communicationRepository;
            _workerManagementService = workerManagementService;
            _taskDistributionService = taskDistributionService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Distribution Service starting...");

            try
            {
                await _communicationRepository.StartAsync();
                _logger.LogInformation("TCP Communication service started on port 8080");

                _communicationRepository.MessageReceived += OnMessageReceived;

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
                if (_communicationRepository != null)
                {
                    _communicationRepository.MessageReceived -= OnMessageReceived;
                    await _communicationRepository.StopAsync();
                    _logger.LogInformation("TCP Communication service stopped");
                }
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
                var idleWorkers = workers.Where(w => w.Status == WorkerStatus.Idle).ToList(); 

                _logger.LogInformation($"Total workers: {workers.Count}, Idle workers: {idleWorkers.Count}");

                var queueCount = await _taskDistributionService.GetQueueCountAsync();
                if (queueCount > 0 && idleWorkers.Any())
                {
                    _logger.LogInformation($"Tasks in queue: {queueCount}");

                    foreach (var worker in idleWorkers)
                    {
                        var nextTask = await _taskDistributionService.GetNextTaskAsync(worker.Id);
                        if (nextTask != null)
                        {
                            _logger.LogInformation($"Assigning task '{nextTask.Command}' to worker {worker.Name}");

                            var taskMessage = $"EXECUTE:{nextTask.Command}:{nextTask.Id}";
                            var sent = await _communicationRepository.SendMessageAsync(worker.Id, taskMessage);

                            if (!sent)
                            {
                                _logger.LogWarning($"Failed to send task to worker {worker.Id}. Marking as disconnected.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in task distribution");
            }
        }

        private async void OnMessageReceived(object? sender, string message)
        {
            try
            {
                _logger.LogInformation($"Received message: {message}");

                var parts = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return;

                var messageType = parts[0].ToUpper();

                switch (messageType)
                {
                    case "REGISTER":
                        await HandleWorkerRegistrationAsync(parts[1]);
                        break;
                    case "RESULT":
                        if (parts.Length >= 4) 
                        {
                            await HandleTaskResultAsync(parts[1], parts[2], string.Join(":", parts.Skip(3)));
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {message}");
            }
        }

        private async Task HandleWorkerRegistrationAsync(string workerName)
        {
            try
            {
                var workerId = await _workerManagementService.AddWorkerAsync(workerName, 0);
                _logger.LogInformation($"Worker '{workerName}' registered with ID: {workerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering worker: {workerName}");
            }
        }

        private async Task HandleTaskResultAsync(string taskIdString, string workerIdString, string result)
        {
            try
            {
                if (Guid.TryParse(taskIdString, out var taskId) && Guid.TryParse(workerIdString, out var workerId))
                {
                    var status = result.StartsWith("ERROR:") ? WorkerTaskStatus.Failed : WorkerTaskStatus.Completed;
                    await _taskDistributionService.UpdateTaskResultAsync(taskId, result, status);

                    _logger.LogInformation($"Task {taskId} from Worker {workerId} completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating task result");
            }
        }

        private async Task HandleWorkerHeartbeatAsync(string workerIdString)
        {
            try
            {
                if (Guid.TryParse(workerIdString, out var workerId))
                {
                    var worker = await _workerManagementService.GetWorkerStatusAsync(workerId);
                    if (worker != null && worker.Status == WorkerStatus.Disconnected)
                    {
                        _logger.LogInformation($"Worker {workerId} reconnected");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling heartbeat from worker {workerIdString}");
            }
        }
    }
}