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
        private static TcpClient? _tcpClient;
        private static NetworkStream? _stream;
        private static ILogger<Program>? _logger;

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

            try
            {
                await ConnectToServerAsync();
                await RegisterWorkerAsync();
                await ListenForTasksAsync();
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
                    _logger.LogInformation($"Worker {_workerName} registered with server");
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

            while (_tcpClient?.Connected == true)
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
                var parts = message.Split(':', 3);
                if (parts.Length == 3 && parts[0] == "EXECUTE")
                {
                    var command = parts[1];
                    var taskId = parts[2];

                    _logger.LogInformation($"Executing command: {command} (Task: {taskId})");

                    var result = await ExecuteCommandAsync(command);
                    await SendResultAsync(taskId, result);
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
                        return $"Error: {error}";
                    }

                    return string.IsNullOrEmpty(output) ? "Command executed successfully" : output.Trim();
                }

                return "Failed to start process";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        private static async Task SendResultAsync(string taskId, string result)
        {
            try
            {
                var resultMessage = $"RESULT:{taskId}:{result}";
                var data = Encoding.UTF8.GetBytes(resultMessage + "\n");

                if (_stream != null)
                {
                    await _stream.WriteAsync(data, 0, data.Length);
                    _logger.LogInformation($"Sent result for task {taskId}: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send result for task {taskId}");
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