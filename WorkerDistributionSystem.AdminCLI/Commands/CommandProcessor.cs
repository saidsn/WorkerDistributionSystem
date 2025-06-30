using Microsoft.Extensions.Logging;
using WorkerDistributionSystem.AdminCLI.Services.Abstract;
using WorkerDistributionSystem.Domain.Interfaces;

namespace WorkerDistributionSystem.AdminCLI.Commands
{
    public class CommandProcessor : ICommandProcessor
    {
        private readonly IServiceController _serviceController;
        private readonly IWorkerService _workerService;
        private readonly IWorkerController _workerController;

        public CommandProcessor(
            IServiceController serviceController,
            IWorkerService workerService,
            IWorkerController workerController,
            ILogger<CommandProcessor> logger)
        {
            _serviceController = serviceController;
            _workerService = workerService;
            _workerController = workerController;
        }

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

        private async Task ProcessWorkerCommand(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Worker command requires subcommand");
            }

            var subCommand = args[1].ToLower();
            //var workerName = args[2];
            switch (subCommand)
            {
                case "execute":
                    if (args.Length < 3)
                    {
                        throw new ArgumentException("Execute command requires command text");
                    }
                    await _workerController.ExecuteCommandAsync(args[2]);
                    break;
                case "add":
                    await _workerService.AddAsync("sa",1);
                    break;
                case "remove":
                    await _workerService.RemoveAsync(new Guid());
                    break;
                case "status":
                    var status = await _workerService.GetAllAsync();
                    Console.WriteLine(status[0].Name);
                    break;
                default:
                    throw new ArgumentException($"Unknown worker subcommand: {subCommand}");
            }
        }

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
                    await _serviceController.StartServiceAsync();
                    break;
                case "stop":
                    await _serviceController.StopServiceAsync();
                    break;
                case "status":
                    var status = await _serviceController.GetServiceStatusAsync();
                    Console.WriteLine(status);
                    break;
                default:
                    throw new ArgumentException($"Unknown service subcommand: {subCommand}");
            }
        }
    }
}
