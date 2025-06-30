using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.WorkerApp.Services.Interfaces;

namespace WorkerDistributionSystem.Worker.Services
{
    public class WorkerService : BackgroundService
    {
        private readonly IWorkerApiService _apiService;
        private readonly ICommandExecutor _commandExecutor;
        private readonly ILogger<WorkerService> _logger;
        private readonly IHostApplicationLifetime _lifetime;

        private WorkerDto? _workerInfo;
        private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _taskPollingInterval = TimeSpan.FromSeconds(5);

        public WorkerService(
            IWorkerApiService apiService,
            ICommandExecutor commandExecutor,
            ILogger<WorkerService> logger,
            IHostApplicationLifetime lifetime)
        {
            _apiService = apiService;
            _commandExecutor = commandExecutor;
            _logger = logger;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Register worker
                await RegisterWorkerAsync(stoppingToken);

                if (_workerInfo == null)
                {
                    _logger.LogError("Failed to register worker, shutting down");
                    _lifetime.StopApplication();
                    return;
                }

                // Start heartbeat and task processing loops
                var heartbeatTask = StartHeartbeatLoop(stoppingToken);
                var taskProcessingTask = StartTaskProcessingLoop(stoppingToken);

                // Wait for cancellation
                await Task.WhenAny(heartbeatTask, taskProcessingTask);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker service is stopping...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in worker service");
            }
            finally
            {
                await UnregisterWorkerAsync();
            }
        }

        private async Task RegisterWorkerAsync(CancellationToken cancellationToken)
        {
            var workerName = $"Worker-{Environment.MachineName}-{Environment.ProcessId}";
            var maxRetries = 5;
            var retryDelay = TimeSpan.FromSeconds(5);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogInformation("Attempting to register worker (attempt {Attempt}/{MaxRetries})",
                    attempt, maxRetries);

                _workerInfo = await _apiService.RegisterWorkerAsync(workerName);

                if (_workerInfo != null)
                {
                    _logger.LogInformation("Worker registered successfully with ID: {WorkerId}", _workerInfo.Id);
                    Console.WriteLine($"Worker registered: {_workerInfo.Id}");
                    Console.WriteLine($"Name: {_workerInfo.Name}");
                    Console.WriteLine($"Process ID: {_workerInfo.ProcessId}");
                    Console.WriteLine("Waiting for tasks...");
                    return;
                }

                if (attempt < maxRetries)
                {
                    _logger.LogWarning("Registration failed, retrying in {Delay} seconds...", retryDelay.TotalSeconds);
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }

            _logger.LogError("Failed to register worker after {MaxRetries} attempts", maxRetries);
        }

        private async Task StartHeartbeatLoop(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting heartbeat loop");

            while (!cancellationToken.IsCancellationRequested && _workerInfo != null)
            {
                try
                {
                    await _apiService.SendHeartbeatAsync(_workerInfo.Id);
                    await Task.Delay(_heartbeatInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in heartbeat loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }

            _logger.LogInformation("Heartbeat loop stopped");
        }

        private async Task StartTaskProcessingLoop(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting task processing loop");

            while (!cancellationToken.IsCancellationRequested && _workerInfo != null)
            {
                try
                {
                    // Get a task from the service
                    var task = await _apiService.GetTaskAsync(_workerInfo.Id);

                    if (task != null)
                    {
                        await ProcessTaskAsync(task);
                    }
                    else
                    {
                        // No tasks available, wait before checking again
                        await Task.Delay(_taskPollingInterval, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in task processing loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }

            _logger.LogInformation("Task processing loop stopped");
        }

        private async Task ProcessTaskAsync(WorkerTaskDto task)
        {
            _logger.LogInformation("Processing task {TaskId}: {Command}", task.Id, task.Command);
            Console.WriteLine($"Processing task: {task.Command}");

            try
            {
                // Execute the command
                var result = await _commandExecutor.ExecuteCommandAsync(task.Command);

                if (result.Success)
                {
                    var output = string.IsNullOrEmpty(result.Output) ? "Command completed successfully" : result.Output;
                    await _apiService.CompleteTaskAsync(task.Id, output);

                    Console.WriteLine($"Task completed: {task.Command}");
                    Console.WriteLine($"Output: {output}");
                }
                else
                {
                    var error = string.IsNullOrEmpty(result.Error) ? "Command failed" : result.Error;
                    await _apiService.FailTaskAsync(task.Id, error);

                    Console.WriteLine($"Task failed: {task.Command}");
                    Console.WriteLine($"Error: {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task {TaskId}", task.Id);
                await _apiService.FailTaskAsync(task.Id, $"Processing error: {ex.Message}");

                Console.WriteLine($"Task error: {task.Command}");
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        private async Task UnregisterWorkerAsync()
        {
            if (_workerInfo != null)
            {
                _logger.LogInformation("Unregistering worker {WorkerId}", _workerInfo.Id);
                await _apiService.UnregisterWorkerAsync(_workerInfo.Id);
                Console.WriteLine("Worker unregistered");
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker service is starting...");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}