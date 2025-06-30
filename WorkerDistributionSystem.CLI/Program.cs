using System.Diagnostics;
using System.Text;
using System.Text.Json;
using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Domain.Entities;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.CLI
{
    class Program
    {
      //161feb0b-80e7-4028-8aed-ddb3894ab294

        private static readonly HttpClient _httpClient = new HttpClient();
        private const string _baseUrl = "http://localhost:5000/api"; 

        static async Task Main(string[] args)
        {
            Console.WriteLine("Worker Distribution System CLI");
            Console.WriteLine("==============================");
            Console.WriteLine("Type 'help' for available commands or 'exit' to quit");
            Console.WriteLine();

            while (true)
            {
                Console.Write("WDS> ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                var command = input.Trim().ToLower();

                if (command == "exit" || command == "quit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                if (command == "clear")
                {
                    Console.Clear();
                    continue;
                }

                try
                {
                    await ProcessCommand(input.Trim());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                Console.WriteLine();
            }
        }

        static async Task ProcessCommand(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var command = parts[0].ToLower();

            switch (command)
            {
                case "help":
                    ShowHelp();
                    break;

                case "worker":
                    if (parts.Length > 1)
                        await HandleWorkerCommand(parts);
                    else
                        Console.WriteLine("Usage: worker [execute|add|remove|status] <args>");
                    break;

                case "service":
                    if (parts.Length > 1)
                        await HandleServiceCommand(parts);
                    else
                        Console.WriteLine("Usage: service [start|stop|status]");
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Type 'help' for available commands");
                    break;
            }
        }

        static async Task HandleWorkerCommand(string[] parts)
        {
            var action = parts[1].ToLower();

            switch (action)
            {
                case "execute":
                    if (parts.Length > 2)
                    {
                        var commandToExecute = string.Join(" ", parts.Skip(2));
                        await ExecuteTask(commandToExecute);
                    }
                    else
                    {
                        Console.WriteLine("Usage: worker execute <command>");
                        Console.WriteLine("Example: worker execute \"whoami\"");
                    }
                    break;

                case "add":
                    await SpawnWorker();
                    break;

                case "remove":
                    if (parts.Length > 2)
                        await RemoveWorker(parts[2]);
                    else
                        Console.WriteLine("Usage: worker remove <worker-id>");
                    break;

                case "status":
                    await ShowWorkerStatus();
                    break;

                default:
                    Console.WriteLine("Usage: worker [execute|add|remove|status] <args>");
                    break;
            }
        }

        static async Task HandleServiceCommand(string[] parts)
        {
            var action = parts[1].ToLower();

            switch (action)
            {
                case "start":
                    await StartService();
                    break;

                case "stop":
                    await StopService();
                    break;

                case "status":
                    await ShowServiceStatus();
                    break;

                default:
                    Console.WriteLine("Usage: service [start|stop|status]");
                    break;
            }
        }

        static async Task ExecuteTask(string command)
        {
            try
            {
                var taskData = new { Command = command };
                var json = JsonSerializer.Serialize(taskData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/tasks", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var task = JsonSerializer.Deserialize<WorkerTaskDto>(responseContent,
                               new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Console.WriteLine("✓Task created and queued for execution!");
                    Console.WriteLine($"Task ID: {task.Id}");
                    Console.WriteLine($"Command: {task.Command}");
                    Console.WriteLine($"Status: {GetTaskStatusIcon(task.Status)} {task.Status}");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Failed to create task: {response.StatusCode}");
                    Console.WriteLine($"Error: {error}");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Cannot connect to service. Make sure the service is running.");
                Console.WriteLine($"Service URL: {_baseUrl}");
            }
        }

        static async Task SpawnWorker()
        {
            try
            {
                var workerPath = "WorkerDistributionSystem.Worker.exe";

                if (!File.Exists(workerPath))
                {
                    workerPath = Path.Combine("..", "WorkerDistributionSystem.Worker", "bin", "Debug", "net8.0", "WorkerDistributionSystem.Worker.exe");
                }

                if (!File.Exists(workerPath))
                {
                    Console.WriteLine("✗ Worker executable not found.");
                    Console.WriteLine("  Please build the WorkerDistributionSystem.Worker project first.");
                    Console.WriteLine($"  Expected path: {workerPath}");
                    return;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = workerPath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                var process = Process.Start(processInfo);

                if (process != null)
                {
                    Console.WriteLine("✓ Worker instance spawned successfully!");
                    Console.WriteLine($"  Process ID: {process.Id}");
                    Console.WriteLine("  Worker will connect to service automatically.");

                    await Task.Delay(2000);
                    Console.WriteLine("\nChecking workers after spawn:");
                    await ShowWorkerStatus();
                }
                else
                {
                    Console.WriteLine("✗ Failed to spawn worker instance.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to spawn worker: {ex.Message}");
            }
        }

        static async Task RemoveWorker(string workerId)
        {
            if (!Guid.TryParse(workerId, out var id))
            {
                Console.WriteLine("✗ Invalid worker ID format. Use GUID format.");
                return;
            }

            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/workers/{id}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✓ Worker {workerId} removed successfully");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to remove worker: {response.StatusCode}");
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"  Error: {error}");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("✗ Cannot connect to service.");
            }
        }

        static async Task StartService()
        {
            Console.WriteLine("Starting Windows Service...");
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "start \"WorkerDistributionService\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(processInfo);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    Console.WriteLine("✓ Service started successfully");
                }
                else
                {
                    Console.WriteLine("✗ Failed to start service");
                    Console.WriteLine("  Alternative: Run 'dotnet run' in service project directory");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error starting service: {ex.Message}");
                Console.WriteLine("Alternative: Run 'dotnet run' in service project directory");
            }
        }

        static async Task StopService()
        {
            Console.WriteLine("Stopping Windows Service and all workers...");
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "sc",
                    Arguments = "stop \"WorkerDistributionService\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(processInfo);
                process?.WaitForExit();

                if (process?.ExitCode == 0)
                {
                    Console.WriteLine("✓ Service stopped successfully");
                }
                else
                {
                    Console.WriteLine("✗ Failed to stop service");
                    Console.WriteLine("  Alternative: Use Ctrl+C in service console");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error stopping service: {ex.Message}");
                Console.WriteLine("Alternative: Use Ctrl+C in service console or Task Manager");
            }
        }

        static async Task ShowServiceStatus()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/service/status");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var status = JsonSerializer.Deserialize<ServiceStatusDto>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Console.WriteLine("Service Status:");
                    Console.WriteLine($"Connected Workers: {status.ConnectedWorkers}");
                    Console.WriteLine($"Jobs in Queue: {status.PendingTasks}");
                    Console.WriteLine($"Running: {(status.IsRunning ? "Yes" : "No")}");
                    Console.WriteLine($"Start Time: {status.ServiceStartTime:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"Uptime: {DateTime.UtcNow - status.ServiceStartTime:dd\\:hh\\:mm\\:ss}");
                    Console.WriteLine();
                    Console.WriteLine("Detailed Stats:");
                    Console.WriteLine($"  Busy Workers: {status.BusyWorkers}");
                    Console.WriteLine($"  Idle Workers: {status.IdleWorkers}");
                    Console.WriteLine($"  Completed Tasks: {status.CompletedTasks}");
                    Console.WriteLine($"  Failed Tasks: {status.FailedTasks}");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to get service status: {response.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("✗ Cannot connect to service.");
            }
        }

        static async Task ShowWorkerStatus()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/workers");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var workers = JsonSerializer.Deserialize<List<WorkerDto>>(content, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (workers?.Any() == true)
                    {
                        Console.WriteLine("Worker-specific status about jobs in queue:");
                        Console.WriteLine("==========================================");

                        foreach (var worker in workers)
                        {
                            Console.WriteLine($"Worker ID: {worker.Id}");
                            Console.WriteLine($"Name: {worker.Name}");
                            Console.WriteLine($"Status: {GetStatusIcon(worker.Status)} {worker.Status}");
                            //Console.WriteLine($"  Task: {GetTaskStatusIcon(worker.)} {task.Command}");
                            Console.WriteLine($"Active Tasks: {worker.ActiveTasksCount}");
                            Console.WriteLine($"Connected: {worker.ConnectedAt:yyyy-MM-dd HH:mm:ss}");
                            Console.WriteLine("---");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No workers connected.");
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Failed to get workers: {response.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("✗ Cannot connect to service.");
            }
        }

        static string GetStatusIcon(WorkerStatus status)
        {
            return status switch
            {
                WorkerStatus.Idle => "🟢",
                WorkerStatus.Busy => "🔵",
                WorkerStatus.Disconnected => "🔴",
                _ => "⚪"
            };
        }

        static string GetTaskStatusIcon(WorkerTaskStatus status)  
        {
            return status switch
            {
                WorkerTaskStatus.Pending => "⏳",
                WorkerTaskStatus.InProgress => "⚙️",
                WorkerTaskStatus.Completed => "✅",
                WorkerTaskStatus.Failed => "❌",
                _ => "⚪"
            };
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available Commands (as per assignment requirements):");
            Console.WriteLine("==================================================");
            Console.WriteLine();
            Console.WriteLine("1. worker execute <command>  - Execute command via workers");
            Console.WriteLine("   Example: worker execute \"whoami\"");
            Console.WriteLine();
            Console.WriteLine("2. worker add               - Spawn a worker instance");
            Console.WriteLine();
            Console.WriteLine("3. worker remove <id>       - Remove a worker instance");
            Console.WriteLine("   Example: worker remove 12345678-1234-1234-1234-123456789012");
            Console.WriteLine();
            Console.WriteLine("4. worker status            - Show worker-specific status about jobs in queue");
            Console.WriteLine();
            Console.WriteLine("5. service start            - Start the Windows service");
            Console.WriteLine();
            Console.WriteLine("6. service stop             - Stop the Windows service and all workers");
            Console.WriteLine();
            Console.WriteLine("7. service status           - Print connected workers and jobs in queue");
            Console.WriteLine();
            Console.WriteLine("Additional Commands:");
            Console.WriteLine("  help                      - Show this help");
            Console.WriteLine("  clear                     - Clear console");
            Console.WriteLine("  exit/quit                 - Exit application");
        }
    }

}