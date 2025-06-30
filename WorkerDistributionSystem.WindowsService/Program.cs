using WorkerDistributionSystem.Application.Services;
using WorkerDistributionSystem.Domain.Interfaces;
using WorkerDistributionSystem.Infrastructure.Communication;
using WorkerDistributionSystem.Infrastructure.Repositories;

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
            services.AddSingleton<IWorkerService, WorkerService>();
            services.AddSingleton<ITaskQueue, InMemoryTaskQueue>();
            services.AddSingleton<ICommunicationService, TcpCommunicationService>();

            services.AddTransient<WorkerManagementService>();
            services.AddTransient<TaskDistributionService>();

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
        });

        return host;
    }
}


