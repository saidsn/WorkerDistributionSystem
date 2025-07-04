using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace WorkerDistributionSystem.AdminCLI.Commands
{
    public class CommandProcessor : ICommandProcessor
    {

        #region Constants

        private const string ServicePath = "ServiceSettings:ServiceExePath";

        private const string WorkerPath = "ServiceSettings:WorkerExePath";

        #endregion

        private Process? _serviceProcess;

        private readonly Dictionary<string, Process> _workerProcesses = new();

        private readonly string _serviceExePath;

        private readonly string _workerExePath;

        private readonly IConfiguration _configuration;

        public CommandProcessor(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(IConfiguration));
            _serviceExePath = Path.GetFullPath(_configuration[ServicePath]!);
            _workerExePath = Path.GetFullPath(_configuration[WorkerPath]!);
        }
        public async Task ProcessAsync(string[] args)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("No command provided");
            }

            var command = args[0].ToLower();
            switch (command)
            {
                case "worker":
                    await ProcessWorkerCommand(args);
                    break;
                case "service":
                    await ProcessServiceCommand(args);
                    break;
                default:
                    throw new ArgumentException($"Unknown command: {command}");
            }
        }

        private async Task ProcessServiceCommand(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Service command requires subcommand");
            }

            var subCommand = args[1].ToLower();

            switch (subCommand)
            {
                case "start":
                    await StartServiceAsync();
                    break;
                case "stop":
                    await StopServiceAsync();
                    break;
                case "status":
                    await GetServiceStatusAsync();
                    break;
                default:
                    throw new ArgumentException($"Unknown service subcommand: {subCommand}");
            }
        }

        private async Task StartServiceAsync()
        {
            try
            {
                if (_serviceProcess != null && !_serviceProcess.HasExited)
                {
                    Console.WriteLine("Service is already running!");
                    return;
                }

                var serviceProcessInfo = new ProcessStartInfo
                {
                    FileName = _serviceExePath,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                _serviceProcess = Process.Start(serviceProcessInfo);

                if (_serviceProcess != null)
                {
                    Console.WriteLine($"Windows Service started successfully (PID: {_serviceProcess.Id})");
                    await Task.Delay(3000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start service: {ex.Message}");
            }
        }

        private async Task StopServiceAsync()
        {
            try
            {
                Console.WriteLine("Stopping Windows Service and all workers...");

                foreach (var kvp in _workerProcesses.ToList())
                {
                    var workerName = kvp.Key;
                    var workerProcess = kvp.Value;

                    if (!workerProcess.HasExited)
                    {
                        Console.WriteLine($"Stopping worker '{workerName}'...");
                        workerProcess.Kill();
                        workerProcess.WaitForExit(5000);
                    }

                    _workerProcesses.Remove(workerName);
                }

                if (_serviceProcess != null && !_serviceProcess.HasExited)
                {
                    Console.WriteLine("Stopping Windows Service...");
                    _serviceProcess.Kill();
                    _serviceProcess.WaitForExit(5000);
                    _serviceProcess = null;
                }

                Console.WriteLine("Windows Service and all workers stopped successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping service: {ex.Message}");
            }
        }

        private async Task GetServiceStatusAsync()
        {
            if (_serviceProcess == null || _serviceProcess.HasExited)
            {
                Console.WriteLine("SERVICE STATUS: NOT RUNNING");
                Console.WriteLine($"Connected Workers: 0");
                return;
            }

            Console.WriteLine("SERVICE STATUS: RUNNING");
            Console.WriteLine($"Service PID: {_serviceProcess.Id}");
            Console.WriteLine($"Connected Workers: {_workerProcesses.Count}");

            foreach (var kvp in _workerProcesses)
            {
                var workerName = kvp.Key;
                var process = kvp.Value;
                Console.WriteLine($"  - {workerName} (PID: {process.Id}) - {(process.HasExited ? "STOPPED" : "RUNNING")}");
            }
        }

        private async Task ProcessWorkerCommand(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Worker command requires subcommand");
            }

            var subCommand = args[1].ToLower();

            switch (subCommand)
            {
                case "add":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Worker name is required. Usage: worker add <worker_name>");
                        return;
                    }
                    await AddWorkerAsync(args[2]);
                    break;

                case "remove":
                    if (args.Length == 3 && args[2].ToLower() == "all")
                    {
                        await RemoveAllWorkersAsync();
                        return;
                    }

                    if (args.Length < 3)
                    {
                        Console.WriteLine("Worker name is required. Usage: worker remove <worker_name> or worker remove all");
                        return;
                    }

                    await RemoveWorkerAsync(args[2]);
                    break;

                case "execute":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Execute command requires a command string");
                        return;
                    }
                    await ExecuteCommandAsync(args[2]);
                    break;

                case "status":
                    if (args.Length == 3)
                    {
                        await ShowWorkerStatusAsync(args[2]);
                    }
                    else
                    {
                        await ShowAllWorkerStatusAsync();
                    }
                    break;

                default:
                    throw new ArgumentException($"Unknown worker subcommand: {subCommand}");
            }
        }

        private async Task AddWorkerAsync(string workerName)
        {
            try
            {
                if (_serviceProcess == null || _serviceProcess.HasExited)
                {
                    Console.WriteLine("Service is not running! Please start service first.");
                    return;
                }

                if (_workerProcesses.ContainsKey(workerName))
                {
                    Console.WriteLine($"Worker '{workerName}' already exists!");
                    return;
                }

                var workerProcessInfo = new ProcessStartInfo
                {
                    FileName = _workerExePath,
                    Arguments = workerName,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                var workerProcess = Process.Start(workerProcessInfo);

                if (workerProcess != null)
                {
                    _workerProcesses[workerName] = workerProcess;
                    Console.WriteLine($"Worker '{workerName}' started successfully (PID: {workerProcess.Id})");
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add worker: {ex.Message}");
            }
        }

        private async Task RemoveWorkerAsync(string workerName)
        {
            try
            {
                if (_workerProcesses.TryGetValue(workerName, out var workerProcess))
                {
                    if (!workerProcess.HasExited)
                    {
                        Console.WriteLine($"Stopping worker '{workerName}'...");
                        workerProcess.Kill();
                        workerProcess.WaitForExit(5000);
                    }

                    _workerProcesses.Remove(workerName);
                    Console.WriteLine($"Worker '{workerName}' stopped successfully");
                }
                else
                {
                    Console.WriteLine($"Worker '{workerName}' not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing worker: {ex.Message}");
            }
        }

        private async Task RemoveAllWorkersAsync()
        {
            try
            {
                if (_workerProcesses.Count == 0)
                {
                    Console.WriteLine("No workers found");
                    return;
                }

                Console.WriteLine("Stopping all workers...");

                foreach (var kvp in _workerProcesses.ToList())
                {
                    var workerName = kvp.Key;
                    var workerProcess = kvp.Value;

                    if (!workerProcess.HasExited)
                    {
                        Console.WriteLine($"Stopping worker '{workerName}'...");
                        workerProcess.Kill();
                        workerProcess.WaitForExit(5000);
                    }

                    _workerProcesses.Remove(workerName);
                }

                Console.WriteLine("All workers removed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing all workers: {ex.Message}");
            }
        }

        private async Task ExecuteCommandAsync(string command)
        {
            try
            {
                using var tcpClient = new TcpClient
                {
                    ReceiveTimeout = 60000,
                    SendTimeout = 10000
                };

                await tcpClient.ConnectAsync("127.0.0.1", 8080);

                var stream = tcpClient.GetStream();
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream) { AutoFlush = true };

                await writer.WriteLineAsync($"ADMIN_EXECUTE:{command}");

                var firstResponse = await reader.ReadLineAsync();
                Console.WriteLine($"First response: {firstResponse}");

                if (firstResponse?.StartsWith("TASK_QUEUED:") == true)
                {
                    Console.WriteLine("Task queued successfully. Waiting for result...");

                    string secondResponse = null;
                    var attempts = 0;
                    const int maxAttempts = 10;

                    while (attempts < maxAttempts)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line?.StartsWith("RESULT:") == true)
                        {
                            secondResponse = line;
                            break;
                        }
                        attempts++;
                        await Task.Delay(500);
                    }

                    if (secondResponse?.StartsWith("RESULT:") == true)
                    {
                        try
                        {
                            var base64Result = secondResponse.Substring(7);

                            var padding = base64Result.Length % 4;
                            if (padding != 0)
                            {
                                base64Result += new string('=', 4 - padding);
                            }

                            var resultBytes = Convert.FromBase64String(base64Result);
                            var decodedResult = Encoding.UTF8.GetString(resultBytes);

                            Console.WriteLine("Command Result:");
                            Console.WriteLine(decodedResult);
                        }
                        catch (FormatException ex)
                        {
                            Console.WriteLine("Invalid Base64 result received.");
                            Console.WriteLine($"Error: {ex.Message}");
                            Console.WriteLine($"Base64 length: {secondResponse.Length - 7}");
                            Console.WriteLine($"First 100 chars: {secondResponse.Substring(0, Math.Min(100, secondResponse.Length))}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No RESULT response received after multiple attempts.");
                    }
                }
                else if (firstResponse?.StartsWith("ERROR:") == true)
                {
                    Console.WriteLine($"Error: {firstResponse.Substring("ERROR:".Length)}");
                }
                else
                {
                    Console.WriteLine($"Unexpected first response: {firstResponse}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Execute command failed: {ex.Message}");
            }
        }
        private async Task ShowAllWorkerStatusAsync()
        {
            if (_workerProcesses.Count == 0)
            {
                Console.WriteLine("No workers found");
                return;
            }

            Console.WriteLine("Workers Status:");
            foreach (var kvp in _workerProcesses)
            {
                var workerName = kvp.Key;
                var process = kvp.Value;
                Console.WriteLine($"  - {workerName} (PID: {process.Id}) - {(process.HasExited ? "STOPPED" : "RUNNING")}");
            }
        }

        private async Task ShowWorkerStatusAsync(string workerName)
        {
            if (_workerProcesses.TryGetValue(workerName, out var process))
            {
                Console.WriteLine($"  - {workerName} (PID: {process.Id}) - {(process.HasExited ? "STOPPED" : "RUNNING")}");
            }
            else
            {
                Console.WriteLine($"Worker '{workerName}' not found");
            }
        }
    }
}
