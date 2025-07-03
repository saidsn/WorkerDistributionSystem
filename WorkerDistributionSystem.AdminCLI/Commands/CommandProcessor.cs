using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WorkerDistributionSystem.AdminCLI.Commands
{
    public class CommandProcessor : ICommandProcessor
    {
        private Process? _serviceProcess;
        private readonly Dictionary<string, Process> _workerProcesses = new();
        private readonly string _serviceExePath = Path.Combine("..", "..", "..", "..", "WorkerDistributionSystem.WindowsService", "bin", "Debug", "net8.0", "WorkerDistributionSystem.WindowsService.exe");
        private readonly string _workerExePath = Path.Combine("..", "..", "..", "..", "WorkerDistributionSystem.WorkerApp", "bin", "Debug", "net8.0", "WorkerDistributionSystem.WorkerApp.exe");

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

        //SERVICE-RELATED METHODS
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

                // 1. Bütün Worker proseslərini öldür
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

                // 2. Service prosesini öldür
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

        // WORKER-RELATED METHODS
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
                    //Console.WriteLine($"Worker '{workerName}' stopped");
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
                //Console.WriteLine($"[DEBUG] Connecting to service...");

                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync("127.0.0.1", 8080);
                //Console.WriteLine($"[DEBUG] Connected.");

                var stream = tcpClient.GetStream();
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream) { AutoFlush = true };

                // ✅ Message gönder
                await writer.WriteLineAsync($"ADMIN_EXECUTE:{command}");
                //Console.WriteLine($"[DEBUG] Message sent: ADMIN_EXECUTE:{command}");

                // ✅ İlk response'u bekle (TASK_QUEUED)
                var firstResponse = await reader.ReadLineAsync();
                //Console.WriteLine($"[DEBUG] First response: {firstResponse}");

                if (firstResponse?.StartsWith("TASK_QUEUED:") == true)
                {
                    Console.WriteLine("Task queued successfully. Waiting for result...");

                    // ✅ İkinci response'u bekle (RESULT)
                    var secondResponse = await reader.ReadLineAsync();
                    //Console.WriteLine($"[DEBUG] Second response: {secondResponse}");

                    if (secondResponse?.StartsWith("RESULT:") == true)
                    {
                        var result = secondResponse.Substring("RESULT:".Length);
                        Console.WriteLine($"Command Result: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Exception: {ex.Message}");
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

        //private async Task HandleTaskResultAsync()
        //{
        //    if (_workerProcesses.TryGetValue(taskId, out var adminClient))
        //    {
        //        var adminStream = adminClient.GetStream();
        //        var writer = new StreamWriter(adminStream) { AutoFlush = true };

        //        await writer.WriteLineAsync($"RESULT:{result}");

        //        _adminClients.Remove(taskId); // artıq lazım deyil
        //    }
        //}
    }
}