using WorkerDistributionSystem.Application.Services.Interfaces;

namespace WorkerDistributionSystem.AdminCLI.Controllers
{
    public class WindowsServiceController
    {
        private readonly IServiceStatusService _serviceStatusService;

        public WindowsServiceController(IServiceStatusService serviceStatusService)
        {
            _serviceStatusService = serviceStatusService;
        }

        public async Task StartServiceAsync()
        {
            await _serviceStatusService.StartServiceAsync();
        }

        public async Task StopServiceAsync()
        {
            await _serviceStatusService.StopServiceAsync();
        }

        public async Task<string> GetServiceStatusAsync()
        {
            var dto = await _serviceStatusService.GetStatusAsync();
            var result = @$"SERVICE STATUS" +
                   $"\nRunning: {(dto.IsRunning ? "YES" : "NO")}\n" +
                   $"Connected Workers: {dto.ConnectedWorkers}\n" +
                   $"Tasks in Queue: {dto.TotalTasksInQueue}\n" +
                   $"Uptime: {DateTime.Now - dto.ServiceStartTime:hh\\:mm\\:ss}";

            Console.WriteLine(result);

            return result;
        }
    }
}