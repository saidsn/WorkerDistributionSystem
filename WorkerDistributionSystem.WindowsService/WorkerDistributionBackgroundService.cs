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
        _communicationRepository = communicationRepository;
        _workerManagementService = workerManagementService;
        _taskDistributionService = taskDistributionService;
        _serviceStatusService = serviceStatusService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Worker Distribution Service starting...");

        try
        {
            await _serviceStatusService.SetServiceRunningAsync(true);

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
            Console.WriteLine("[DEBUG] MonitorAndDistributeTasksAsync started");

            var workers = await _workerManagementService.GetAllWorkersAsync();
            var idleWorkers = workers.Where(w => w.Status == WorkerStatus.Idle).ToList();

            Console.WriteLine($"[DEBUG] Total workers: {workers.Count}, Idle workers: {idleWorkers.Count}");

            var queueCount = await _taskDistributionService.GetQueueCountAsync();
            Console.WriteLine($"[DEBUG] Tasks in queue: {queueCount}");

            if (queueCount > 0 && idleWorkers.Any())
            {
                Console.WriteLine($"[DEBUG] Processing tasks from queue...");

                foreach (var worker in idleWorkers)
                {
                    var nextTask = await _taskDistributionService.GetNextTaskAsync(worker.Id);
                    if (nextTask != null)
                    {
                        Console.WriteLine($"[DEBUG] Assigning task '{nextTask.Command}' (ID: {nextTask.Id}) to worker {worker.Name}");

                        var taskMessage = $"EXECUTE:{nextTask.Command}:{nextTask.Id}";
                        Console.WriteLine($"[DEBUG] Sending message to worker: {taskMessage}");

                        var sent = await _communicationRepository.SendMessageAsync(worker.Id, taskMessage);

                        if (sent)
                        {
                            Console.WriteLine($"[DEBUG] Task sent successfully to worker {worker.Name}");
                            await _workerManagementService.UpdateStatusAsync(worker.Id, WorkerStatus.Busy);
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] Failed to send task to worker {worker.Id}. Marking as disconnected.");
                            await _communicationRepository.DisconnectWorkerAsync(worker.Id);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] No tasks available for worker {worker.Name}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] No tasks to process (QueueCount: {queueCount}, IdleWorkers: {idleWorkers.Count})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error in MonitorAndDistributeTasksAsync: {ex.Message}");
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
                        Console.WriteLine($"[SERVICE DEBUG] Processing command: {command}");

                        var allWorkers = await _workerManagementService.GetAllWorkersAsync();
                        Console.WriteLine($"[SERVICE DEBUG] Total workers: {allWorkers.Count}");

                        var idleWorker = allWorkers.FirstOrDefault(w => w.Status == WorkerStatus.Idle);

                        if (idleWorker != null)
                        {
                            Console.WriteLine($"[SERVICE DEBUG] Found idle worker: {idleWorker.Name}");

                            var taskId = await _taskDistributionService.ExecuteCommandAsync(command, idleWorker.Id);
                            Console.WriteLine($"[SERVICE DEBUG] Task created with ID: {taskId}");

                            lock (_adminClients)
                            {
                                _adminClients[taskId] = data.client;
                                Console.WriteLine($"[SERVICE DEBUG] AdminCLI client registered for task {taskId}");
                            }

                            var stream = data.client.GetStream();
                            var response = Encoding.UTF8.GetBytes($"TASK_QUEUED:{taskId}\n");
                            await stream.WriteAsync(response, 0, response.Length);
                            Console.WriteLine($"[SERVICE DEBUG] Sent TASK_QUEUED response");
                        }
                        else
                        {
                            Console.WriteLine($"[SERVICE DEBUG] No idle workers available");
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
            Console.WriteLine($"[DEBUG] HandleTaskResultAsync called with TaskId: {taskIdString}, WorkerId: {workerIdString}");

            if (Guid.TryParse(taskIdString, out var taskId) && Guid.TryParse(workerIdString, out var workerId))
            {
                var status = result.StartsWith("ERROR:") ? WorkerTaskStatus.Failed : WorkerTaskStatus.Completed;

                await _taskDistributionService.UpdateTaskResultAsync(taskId, result, status);
                await _workerManagementService.UpdateStatusAsync(workerId, WorkerStatus.Idle);

                _logger.LogInformation($"Task {taskId} completed, Worker {workerId} set to Idle");

                // ✅ Admin client-i lock içində al
                TcpClient? adminClient = null;
                lock (_adminClients)
                {
                    if (_adminClients.TryGetValue(taskId, out adminClient))
                    {
                        _adminClients.Remove(taskId);
                        Console.WriteLine($"[DEBUG] Found AdminCLI client for task {taskId}");
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] No AdminCLI client found for task {taskId}");
                        Console.WriteLine($"[DEBUG] Available clients: {string.Join(", ", _adminClients.Keys)}");
                    }
                }

                // ✅ Lock dışında async işlemi yap
                if (adminClient != null)
                {
                    try
                    {
                        Console.WriteLine($"[DEBUG] Sending result back to AdminCLI: {result}");

                        var stream = adminClient.GetStream();
                        var writer = new StreamWriter(stream) { AutoFlush = true };

                        await writer.WriteLineAsync($"RESULT:{result}");

                        Console.WriteLine($"[DEBUG] Result sent successfully");
                        _logger.LogInformation($"Result sent back to AdminCLI: {result}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Error sending result back: {ex.Message}");
                        _logger.LogError(ex, "Error sending result back to AdminCLI");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] Invalid TaskId or WorkerId format");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Exception in HandleTaskResultAsync: {ex.Message}");
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

                    if (worker.Status == WorkerStatus.Disconnected)
                    {
                        _logger.LogInformation($"Worker {workerId} reconnected via heartbeat");
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