using System.Net.Sockets;
using System.Text;
using WorkerDistributionSystem.Application.Events;
using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Domain.Enums;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

public class WorkerDistributionBackgroundService : BackgroundService
{
    private readonly ILogger<WorkerDistributionBackgroundService> _logger;
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IWorkerManagementService _workerManagementService;
    private readonly ITaskDistributionService _taskDistributionService;
    private readonly Dictionary<Guid, TcpClient> _adminClients = new();

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
                            // Worker-i disconnect et
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
            _logger.LogInformation($"Received message: {data.message}");

            var parts = data.message.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            var messageType = parts[0].ToUpper();

            switch (messageType)
            {
                case "REGISTER":
                    // ✅ Worker registration metodunu çağır
                    await HandleWorkerRegistrationAsync(parts[1], data.client);
                    break;

                case "RESULT":
                    // ✅ Task result metodunu çağır
                    if (parts.Length >= 4)
                    {
                        await HandleTaskResultAsync(parts[1], parts[2], string.Join(":", parts.Skip(3)));
                    }
                    break;

                case "HEARTBEAT":
                    // ✅ Heartbeat metodunu çağır
                    if (parts.Length >= 2)
                    {
                        await HandleWorkerHeartbeatAsync(parts[1]);
                    }
                    break;

                case "ADMIN_EXECUTE":
                    if (parts.Length >= 2)
                    {
                        var command = string.Join(":", parts.Skip(1));
                        Console.WriteLine($"[DEBUG] Processing command: {command}");

                        var allWorkers = await _workerManagementService.GetAllWorkersAsync();
                        Console.WriteLine($"[DEBUG] Total workers in service: {allWorkers.Count}");

                        foreach (var worker in allWorkers)
                        {
                            Console.WriteLine($"[DEBUG] Worker: {worker.Name} - Status: {worker.Status}");
                        }

                        var idleWorker = allWorkers.FirstOrDefault(w => w.Status == WorkerStatus.Idle);

                        if (idleWorker != null)
                        {
                            Console.WriteLine($"[DEBUG] Found idle worker: {idleWorker.Name}");

                            var taskId = await _taskDistributionService.ExecuteCommandAsync(command, idleWorker.Id);

                            lock (_adminClients)
                            {
                                _adminClients[taskId] = data.client;
                            }

                            var stream = data.client.GetStream();
                            var response = Encoding.UTF8.GetBytes($"TASK_QUEUED:{command}\n");
                            await stream.WriteAsync(response, 0, response.Length);
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] No idle workers available");

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

            _logger.LogInformation($"Worker '{workerName}' registered with ID: {workerId}");

            await _communicationRepository.SendMessageAsync(workerId, $"REGISTERED:{workerId}");
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

                _logger.LogInformation($"Task {taskId} from Worker {workerId} completed with status: {status}");

                // ✅ Admin client'i lock içinde al, sonra lock dışında kullan
                TcpClient? adminClient = null;

                lock (_adminClients)
                {
                    if (_adminClients.TryGetValue(taskId, out adminClient))
                    {
                        _adminClients.Remove(taskId);
                    }
                }

                // ✅ Lock dışında async işlemi yap
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
                if (worker != null)
                {
                    _logger.LogDebug($"Heartbeat received from worker {workerId}");

                    // Worker-in status-unu yenilə
                    if (worker.Status == WorkerStatus.Disconnected)
                    {
                        _logger.LogInformation($"Worker {workerId} reconnected via heartbeat");
                        // Status-u Connected et
                        // Bu üçün WorkerRepository-də UpdateStatus method lazımdır
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error handling heartbeat from worker {workerIdString}");
        }
    }
}