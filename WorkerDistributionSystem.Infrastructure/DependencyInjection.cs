using Microsoft.Extensions.DependencyInjection;
using WorkerDistributionSystem.Application.Interfaces.Repositories;
using WorkerDistributionSystem.Application.Interfaces.Services;
using WorkerDistributionSystem.Application.Services;
using WorkerDistributionSystem.Infrastructure.Repositories;

namespace WorkerDistributionSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<IWorkerRepository, WorkerRepository>();
            services.AddSingleton<ITaskRepository, TaskRepository>();

            services.AddScoped<IWorkerService, WorkerService>();
            services.AddScoped<ITaskService, TaskService>();

            return services;
        }
    }
}