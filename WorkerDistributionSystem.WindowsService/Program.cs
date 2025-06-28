using WorkerDistributionSystem.Application.Services;
using WorkerDistributionSystem.Domain.Interfaces;
using WorkerDistributionSystem.Infrastructure.Communication;
using WorkerDistributionSystem.Infrastructure.Repositories;

namespace WorkerDistributionSystem.WindowsService;

public class Program
{
    public static void Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IWorkerService, InMemoryWorkerRepository>();
                services.AddSingleton<ITaskQueue, InMemoryTaskQueue>();
                services.AddSingleton<ICommunicationService, TcpCommunicationService>();

                services.AddTransient<WorkerManagementService>();
                services.AddTransient<TaskDistributionService>();

                services.AddHostedService<WorkerDistributionBackgroundService>();
            })
            .UseWindowsService(options =>
            {
                options.ServiceName = "WorkerDistributionService";
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .Build();

        host.Run();
    }
}
