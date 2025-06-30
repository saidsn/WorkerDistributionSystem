using Microsoft.Extensions.Logging;
using WorkerDistributionSystem.AdminCLI.Services.Abstract;

namespace WorkerDistributionSystem.AdminCLI.Services.Concrete
{
    public class WorkerController : IWorkerController
    {
        private readonly ILogger<WorkerController> _logger;

        public WorkerController(ILogger<WorkerController> logger)
        {
            _logger = logger;
        }

        public async Task ExecuteCommandAsync(string command)
        {
            try
            {
                Console.WriteLine($"Executing command on worker: {command}");
                await Task.Delay(500);
                Console.WriteLine($"Command executed successfully: {command}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to execute command: {ex.Message}");
                _logger.LogError(ex, "Failed to execute command");
            }
        }

        public async Task AddWorkerAsync()
        {
            try
            {
                Console.WriteLine("Adding a new worker...");
                await Task.Delay(500); 
                Console.WriteLine("Worker added successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add worker: {ex.Message}");
                _logger.LogError(ex, "Failed to add worker");
            }
        }

        public async Task RemoveWorkerAsync()
        {
            try
            {
                Console.WriteLine("Removing a worker...");
                await Task.Delay(500); 
                Console.WriteLine("Worker removed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove worker: {ex.Message}");
                _logger.LogError(ex, "Failed to remove worker");
            }
        }

        public async Task<string> GetWorkerStatusAsync()
        {
            try
            {
                Console.WriteLine("Fetching worker status...");
                await Task.Delay(500); 
                return "Worker status: All workers are operational.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get worker status: {ex.Message}");
                _logger.LogError(ex, "Failed to get worker status");
                return "Failed to get worker status.";
            }
        }
    }
}
