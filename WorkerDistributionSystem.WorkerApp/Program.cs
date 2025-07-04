using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace WorkerDistributionSystem.WorkerApp
{
    public class Program
    {
        private static string? _workerName;
        private static Guid _workerId = Guid.Empty; // ✅ Əlavə
        private static TcpClient? _tcpClient;
        private static NetworkStream? _stream;
        private static ILogger<Program>? _logger;
        private static CancellationTokenSource _cancellationTokenSource = new();

        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddLogging(configure => configure.AddConsole());
                })
                .Build();

            _logger = host.Services.GetRequiredService<ILogger<Program>>();

            _workerName = args.Length > 0 ? args[0] : $"Worker_{Environment.MachineName}_{DateTime.Now:HHmmss}";

            _logger.LogInformation($"Starting Worker: {_workerName}");

            // ✅ Graceful shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _cancellationTokenSource.Cancel();
            };

            try
            {
                await ConnectToServerAsync();
                await RegisterWorkerAsync();

                // ✅ Heartbeat task başlat
                var heartbeatTask = Task.Run(() => SendHeartbeatAsync(_cancellationTokenSource.Token));

                await ListenForTasksAsync();

                // ✅ Heartbeat task-ı cancel et
                _cancellationTokenSource.Cancel();
                await heartbeatTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker application error");
            }
            finally
            {
                await DisconnectFromServerAsync();
            }

            _logger.LogInformation($"Worker {_workerName} stopped");
        }

        private static async Task ConnectToServerAsync()
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync("127.0.0.1", 8080);
                _stream = _tcpClient.GetStream();

                _logger.LogInformation($"Connected to server on 127.0.0.1:8080");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to server");
                throw;
            }
        }

        private static async Task RegisterWorkerAsync()
        {
            try
            {
                var registerMessage = $"REGISTER:{_workerName}";
                var data = Encoding.UTF8.GetBytes(registerMessage + "\n");

                if (_stream != null)
                {
                    await _stream.WriteAsync(data, 0, data.Length);
                    _logger.LogInformation($"Worker {_workerName} registration request sent");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register worker");
                throw;
            }
        }

        private static async Task ListenForTasksAsync()
        {
            var buffer = new byte[4096];

            while (_tcpClient?.Connected == true && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (_stream != null)
                    {
                        var bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break;

                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        _logger.LogInformation($"Received message: {message}");

                        await ProcessMessageAsync(message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from server");
                    break;
                }
            }
        }

        private static async Task ProcessMessageAsync(string message)
        {
            try
            {
                var parts = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return;

                var messageType = parts[0].ToUpper();

                switch (messageType)
                {
                    case "REGISTERED": // ✅ Əlavə
                        if (parts.Length >= 2 && Guid.TryParse(parts[1], out var workerId))
                        {
                            _workerId = workerId;
                            _logger.LogInformation($"Worker registered with ID: {_workerId}");
                        }
                        break;

                    case "EXECUTE":
                        if (parts.Length >= 3)
                        {
                            var command = parts[1];
                            var taskId = parts[2];

                            Console.WriteLine($"[WORKER DEBUG] Executing command: {command} (Task: {taskId})");

                            var result = await ExecuteCommandAsync(command);
                            Console.WriteLine($"[WORKER DEBUG] Command result: {result}");

                            await SendResultAsync(taskId, result);
                            Console.WriteLine($"[WORKER DEBUG] Result sent for task: {taskId}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {message}");
            }
        }

        private static async Task<string> ExecuteCommandAsync(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();

                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        return $"ERROR: {error}";
                    }

                    return string.IsNullOrEmpty(output) ? "Command executed successfully" : output.Trim();
                }

                return "ERROR: Failed to start process";
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        private static async Task SendResultAsync(string taskId, string result)
        {
            try
            {
                var resultMessage = $"RESULT:{taskId}:{_workerId}:{result}";
                Console.WriteLine($"[DEBUG] Sending result: {resultMessage}");

                var data = Encoding.UTF8.GetBytes(resultMessage + "\n");

                if (_stream != null)
                {
                    await _stream.WriteAsync(data, 0, data.Length);
                    _logger.LogInformation($"Sent result for task {taskId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send result for task {taskId}");
            }
        }

        // ✅ Heartbeat functionality
        private static async Task SendHeartbeatAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _tcpClient?.Connected == true)
            {
                try
                {
                    if (_workerId != Guid.Empty)
                    {
                        var heartbeatMessage = $"HEARTBEAT:{_workerId}";
                        var data = Encoding.UTF8.GetBytes(heartbeatMessage + "\n");

                        if (_stream != null)
                        {
                            await _stream.WriteAsync(data, 0, data.Length);
                            _logger.LogDebug($"Sent heartbeat");
                        }
                    }

                    await Task.Delay(30000, cancellationToken); // 30 saniyə
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending heartbeat");
                    break;
                }
            }
        }

        private static async Task DisconnectFromServerAsync()
        {
            try
            {
                _stream?.Close();
                _tcpClient?.Close();
                _logger.LogInformation("Disconnected from server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from server");
            }

            await Task.CompletedTask;
        }
    }
}