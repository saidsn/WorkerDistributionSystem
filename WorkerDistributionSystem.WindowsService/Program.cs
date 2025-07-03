using WorkerDistributionSystem.Application.Services.Interfaces;
using WorkerDistributionSystem.Application.Services.Implementations;
using WorkerDistributionSystem.Infrastructure.Repositories.Implementations;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.WindowsService;

public static class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureApplicationServices()
            .ConfigureWindowsService()
            .ConfigureApplicationLogging()
            .Build();

        host.Run();
    }

    private static IHostBuilder ConfigureApplicationServices(this IHostBuilder host)
    {
        host.ConfigureServices((context, services) =>
        {
            // Infrastructure Layer
            services.AddSingleton<IWorkerRepository, WorkerRepository>();
            services.AddSingleton<ITaskRepository, TaskRepository>();
            services.AddSingleton<ICommunicationRepository, TcpCommunicationRepository>();
            services.AddSingleton<IServiceStatusRepository, ServiceStatusRepository>();

            // Application Layer
            services.AddSingleton<IWorkerManagementService, WorkerManagementService>();
            services.AddSingleton<ITaskDistributionService, TaskDistributionService>();
            services.AddSingleton<IServiceStatusService, ServiceStatusService>();

            // Background Service
            services.AddHostedService<WorkerDistributionBackgroundService>();
        });

        return host;
    }

    private static IHostBuilder ConfigureWindowsService(this IHostBuilder host)
    {
        host.UseWindowsService(options =>
        {
            options.ServiceName = "WorkerDistributionService";
        });

        return host;
    }

    private static IHostBuilder ConfigureApplicationLogging(this IHostBuilder host)
    {
        host.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddEventLog();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        return host;
    }
}