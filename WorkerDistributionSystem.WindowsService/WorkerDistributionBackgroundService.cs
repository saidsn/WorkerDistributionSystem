using System.Net.Sockets;
using System.Text;
using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

public class WorkerDistributionBackgroundService : BackgroundService
{
    private readonly ILogger<WorkerDistributionBackgroundService> _logger;

    private readonly ICommunicationRepository _communicationRepository;

    private readonly IWorkerManagementService _workerManagementService;

    private readonly ITaskDistributionService _taskDistributionService;

    private readonly IServiceStatusService _serviceStatusService;

    private readonly Dictionary<Guid, TcpClient> _adminClients = new();

    public WorkerDistributionBackgroundService(
        ILogger<WorkerDistributionBackgroundService> logger,
        ICommunicationRepository communicationRepository,
        IWorkerManagementService workerManagementService,
        ITaskDistributionService taskDistributionService,
        IServiceStatusService serviceStatusService)
    {
        _logger = logger;
        _communicationRepository = communicationRepository ?? throw new ArgumentException(nameof(communicationRepository));
        _workerManagementService = workerManagementService ?? throw new ArgumentException(nameof(workerManagementService));
        _taskDistributionService = taskDistributionService ?? throw new ArgumentException(nameof(taskDistributionService));
        _serviceStatusService = serviceStatusService ?? throw new ArgumentException(nameof(serviceStatusService));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker Distribution Service starting...");

        try
        {
            await _serviceStatusService.SetServiceRunningAsync(true);
            await _communicationRepository.StartAsync();
            _communicationRepository.MessageReceivedWithClient += OnMessageReceivedWithClient;

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
            await _serviceStatusService.SetServiceRunningAsync(false);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker Distribution Service stopping...");

        try
        {
            await _serviceStatusService.SetServiceRunningAsync(false);

            if (_communicationRepository != null)
            {
                _communicationRepository.MessageReceivedWithClient -= OnMessageReceivedWithClient;
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
            var queueCount = await _taskDistributionService.GetQueueCountAsync();

            if (queueCount > 0 && idleWorkers.Any())
            {
                foreach (var worker in idleWorkers)
                {
                    var nextTask = await _taskDistributionService.GetNextTaskAsync(worker.Id);
                    if (nextTask != null)
                    {
                        var taskMessage = $"EXECUTE:{nextTask.Command}:{nextTask.Id}";
                        var sent = await _communicationRepository.SendMessageAsync(worker.Id, taskMessage);

                        if (sent)
                        {
                            await _workerManagementService.UpdateStatusAsync(worker.Id, WorkerStatus.Busy);
                        }
                        else
                        {
                            await _communicationRepository.DisconnectWorkerAsync(worker.Id);
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

    private async void OnMessageReceivedWithClient(object? sender, (string message, TcpClient client) data)
    {
        try
        {
            var parts = data.message.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            var messageType = parts[0].ToUpper();

            switch (messageType)
            {
                case "REGISTER":
                    await HandleWorkerRegistrationAsync(parts[1], data.client);
                    break;

                case "RESULT":
                    if (parts.Length >= 4)
                    {
                        await HandleTaskResultAsync(parts[1], parts[2], string.Join(":", parts.Skip(3)));
                    }
                    break;

                case "HEARTBEAT":
                    if (parts.Length >= 2)
                    {
                        await HandleWorkerHeartbeatAsync(parts[1]);
                    }
                    break;

                case "ADMIN_EXECUTE":
                    if (parts.Length >= 2)
                    {
                        var command = string.Join(":", parts.Skip(1));
                        var allWorkers = await _workerManagementService.GetAllWorkersAsync();
                        var idleWorker = allWorkers.FirstOrDefault(w => w.Status == WorkerStatus.Idle);

                        if (idleWorker != null)
                        {
                            var taskId = await _taskDistributionService.ExecuteCommandAsync(command, idleWorker.Id);
                            lock (_adminClients)
                            {
                                _adminClients[taskId] = data.client;
                            }

                            var stream = data.client.GetStream();
                            var response = Encoding.UTF8.GetBytes($"TASK_QUEUED:{taskId}\n");
                            await stream.WriteAsync(response, 0, response.Length);
                        }
                        else
                        {
                            var stream = data.client.GetStream();
                            var response = Encoding.UTF8.GetBytes($"ERROR:No idle workers available\n");
                            await stream.WriteAsync(response, 0, response.Length);
                        }
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing message: {data.message}");
        }
    }

    private async Task HandleWorkerRegistrationAsync(string workerName, TcpClient client)
    {
        try
        {
            var workerId = await _workerManagementService.AddWorkerAsync(workerName, Environment.ProcessId);
            await _workerManagementService.UpdateStatusAsync(workerId, WorkerStatus.Idle);
            _communicationRepository.RegisterWorker(workerId, client);
            await _communicationRepository.SendMessageAsync(workerId, $"REGISTERED:{workerId}");
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
                await _workerManagementService.UpdateStatusAsync(workerId, WorkerStatus.Idle);

                TcpClient? adminClient = null;
                lock (_adminClients)
                {
                    if (_adminClients.TryGetValue(taskId, out adminClient))
                    {
                        _adminClients.Remove(taskId);
                    }
                }

                if (adminClient != null)
                {
                    try
                    {
                        var stream = adminClient.GetStream();
                        var writer = new StreamWriter(stream) { AutoFlush = true };
                        await writer.WriteLineAsync($"RESULT:{result}");
                        _logger.LogInformation($"Result sent back to AdminCLI: {result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending result back to AdminCLI");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task result");
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
                    _logger.LogInformation($"Worker {workerId} reconnected via heartbeat");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling heartbeat from worker {workerIdString}");
        }
    }
}
