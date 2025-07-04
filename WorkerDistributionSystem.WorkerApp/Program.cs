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

        private static Guid _workerId = Guid.Empty;

        private static TcpClient? _tcpClient;

        private static NetworkStream? _stream;

        private static ILogger<Program>? _logger;

        private static CancellationTokenSource _cancellationTokenSource = new();

        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddLogging(cfg => cfg.AddConsole()))
                .Build();

            _logger = host.Services.GetRequiredService<ILogger<Program>>();
            _workerName = args.Length > 0 ? args[0] : $"Worker_{Environment.MachineName}_{DateTime.Now:HHmmss}";

            _logger.LogInformation($"Starting Worker: {_workerName}");

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                _cancellationTokenSource.Cancel();
            };

            try
            {
                await ConnectToServerAsync();
                await RegisterWorkerAsync();

                var heartbeatTask = Task.Run(() => SendHeartbeatAsync(_cancellationTokenSource.Token));
                await ListenForTasksAsync();

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
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync("127.0.0.1", 8080);
            _stream = _tcpClient.GetStream();
            _logger.LogInformation("Connected to server on 127.0.0.1:8080");
        }

        private static async Task RegisterWorkerAsync()
        {
            if (_stream is null) throw new InvalidOperationException("Network stream is null");

            var registerMessage = $"REGISTER:{_workerName}\n";
            var data = Encoding.UTF8.GetBytes(registerMessage);
            await _stream.WriteAsync(data, 0, data.Length);
            _logger.LogInformation($"Worker {_workerName} registration request sent");
        }

        private static async Task ListenForTasksAsync()
        {
            var buffer = new byte[4096];

            while (_tcpClient?.Connected == true && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                int bytesRead = 0;
                try
                {
                    if (_stream is null) break;

                    bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                    if (bytesRead == 0) break;

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    _logger.LogInformation($"Received message: {message}");

                    await ProcessMessageAsync(message);
                }
                catch (OperationCanceledException)
                {
                    break;
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
            var parts = message.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var messageType = parts[0].ToUpperInvariant();

            switch (messageType)
            {
                case "REGISTERED":
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

                        var result = await ExecuteCommandAsync(command);
                        await SendResultAsync(taskId, result);
                    }
                    break;
            }
        }

        private static async Task<string> ExecuteCommandAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(psi);
                if (process == null) return "ERROR: Failed to start process";

                await process.WaitForExitAsync();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(error))
                    return $"ERROR: {error.Trim()}";

                return string.IsNullOrWhiteSpace(output) ? "Command executed successfully" : output.Trim();
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
                var resultBytes = Encoding.UTF8.GetBytes(result);
                var base64Result = Convert.ToBase64String(resultBytes);

                var resultMessage = $"RESULT:{taskId}:{_workerId}:{base64Result}";
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


        private static async Task SendHeartbeatAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _tcpClient?.Connected == true)
            {
                if (_workerId != Guid.Empty && _stream != null)
                {
                    var heartbeatMessage = $"HEARTBEAT:{_workerId}\n";
                    var data = Encoding.UTF8.GetBytes(heartbeatMessage);

                    try
                    {
                        await _stream.WriteAsync(data, 0, data.Length, cancellationToken);
                        _logger?.LogDebug("Sent heartbeat");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error sending heartbeat");
                        break;
                    }
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
                catch (OperationCanceledException)
                {
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
                _logger?.LogInformation("Disconnected from server");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disconnecting from server");
            }

            await Task.CompletedTask;
        }
    }
}
