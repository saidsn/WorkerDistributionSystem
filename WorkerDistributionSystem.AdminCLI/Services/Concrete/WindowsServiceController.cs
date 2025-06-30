using Microsoft.Extensions.Logging;
using System.ServiceProcess;
using WorkerDistributionSystem.AdminCLI.Services.Abstract;

namespace WorkerDistributionSystem.AdminCLI.Services.Concrete;
public class WindowsServiceController : IServiceController
{
    private readonly ILogger<WindowsServiceController> _logger;
    private const string ServiceName = "WorkerDistributionService";

    public WindowsServiceController(ILogger<WindowsServiceController> logger)
    {
        _logger = logger;
    }

    public async Task StartServiceAsync()
    {
        try
        {
            using var service = new ServiceController(ServiceName);

            if (service.Status == ServiceControllerStatus.Running)
            {
                Console.WriteLine($"Service '{ServiceName}' is already running.");
                return;
            }

            Console.WriteLine($"Starting service '{ServiceName}'...");
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));

            Console.WriteLine($"Service '{ServiceName}' started successfully.");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine($"Service '{ServiceName}' not found. Please install the service first.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start service: {ex.Message}");
            throw;
        }
    }

    public async Task StopServiceAsync()
    {
        try
        {
            Console.WriteLine($"Attempting to stop service: {ServiceName}");
            using var service = new ServiceController(ServiceName);

            Console.WriteLine($"Current service status: {service.Status}");

            if (service.Status == ServiceControllerStatus.Stopped)
            {
                Console.WriteLine($"Service '{ServiceName}' is already stopped.");
                return;
            }

            Console.WriteLine($"Stopping service '{ServiceName}'...");
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));

            Console.WriteLine($"Service '{ServiceName}' stopped successfully.");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"InvalidOperationException: {ex.Message}");
            Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
        }
    }

    public async Task<string> GetServiceStatusAsync()
    {
        try
        {
            using var service = new ServiceController(ServiceName);
            return $"Service '{ServiceName}' status: {service.Status}";
        }
        catch (InvalidOperationException)
        {
            return $"Service '{ServiceName}' not found or not installed.";
        }
        catch (Exception ex)
        {
            return $"Failed to get service status: {ex.Message}";
        }
    }
}