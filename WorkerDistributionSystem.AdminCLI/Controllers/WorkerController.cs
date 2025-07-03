using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Domain.Enums;

namespace WorkerDistributionSystem.AdminCLI.Controllers
{
    public class WorkerController
    {
        private readonly ITaskDistributionService _taskDistributionService;
        private readonly IWorkerManagementService _workerManagementService;

        public WorkerController(
            ITaskDistributionService taskDistributionService,
            IWorkerManagementService workerManagementService)
        {
            _taskDistributionService = taskDistributionService;
            _workerManagementService = workerManagementService;
        }

        public async Task<string> ExecuteCommandAsync(string command)
        {
            try
            {
                var workers = await _workerManagementService.GetAllWorkersAsync();
                var availableWorker = workers.FirstOrDefault(w => w.Status == WorkerStatus.Idle);

                if (availableWorker == null)
                {
                    return "No available workers found";
                }

                var taskId = await _taskDistributionService.ExecuteCommandAsync(command, availableWorker.Id);
                return $"Command '{command}' queued successfully with Task ID: {taskId}";
            }
            catch (Exception ex)
            {
                return $"Failed to execute command: {ex.Message}";
            }
        }

        public async Task AddWorkerAsync(string workerName, int processId)
        {
            var workerId = await _workerManagementService.AddWorkerAsync(workerName, processId);
            Console.WriteLine($"Worker '{workerName}' added with ID: {workerId}");
        }

        public async Task RemoveWorkerAsync(Guid workerId)
        {
            var isDeleted = await _workerManagementService.RemoveWorkerAsync(workerId);
            if (isDeleted)
            {
                Console.WriteLine($"Worker {workerId} removed successfully");
            }
            else
            {
                Console.WriteLine($"Worker with ID: {workerId} not found!");
            }
        }

        public async Task ShowAllWorkersAsync()
        {
            var workers = await _workerManagementService.GetAllWorkersAsync();
            if (workers.Count == 0)
            {
                Console.WriteLine("No workers found");
                return;
            }

            Console.WriteLine("Workers Status:");
            foreach (var worker in workers)
            {
                Console.WriteLine($"{worker.Name} (ID: {worker.Id}) - Status: {worker.Status} - Tasks: {worker.TaskCount}");
            }
        }

        public async Task ShowWorkerStatusAsync(Guid workerId)
        {
            var worker = await _workerManagementService.GetWorkerStatusAsync(workerId);
            if (worker == null)
            {
                Console.WriteLine($"Worker {workerId} not found");
                return;
            }

            Console.WriteLine($"Worker: {worker.Name} - Status: {worker.Status} - Tasks: {worker.TaskCount}");
        }
    }
}
