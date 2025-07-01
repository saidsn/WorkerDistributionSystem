using WorkerDistributionSystem.AdminCLI.Commands;
using WorkerDistributionSystem.AdminCLI.Controllers;
using WorkerDistributionSystem.Application.Services.Interfaces;

public class CommandProcessor : ICommandProcessor
{
    private readonly WindowsServiceController _windowsServiceController;
    private readonly WorkerController _workerController;
    private static int _processId = 1;

    public CommandProcessor(
        IServiceStatusService serviceStatusService,
        IWorkerManagementService workerManagementService,
        ITaskDistributionService taskDistributionService)
    {
        _windowsServiceController = new WindowsServiceController(serviceStatusService);
        _workerController = new WorkerController(taskDistributionService, workerManagementService);
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

        switch (subCommand)
        {
            case "execute":
                if (args.Length < 3)
                {
                    Console.WriteLine("Execute command requires a command string");
                    return;
                }
                var executeResult = await _workerController.ExecuteCommandAsync(args[2]);
                Console.WriteLine(executeResult);
                break;

            case "add":
                if(args.Length < 3)
                {
                    Console.WriteLine("Worker name shouldn't be empty");
                    break;
                }
                var workerName = args[2].ToLower();
                await _workerController.AddWorkerAsync(workerName, _processId);
                _processId++;
                break;

            case "remove":
                if (args.Length < 3)
                {
                    Console.WriteLine("Remove command requires worker ID");
                    return;
                }

                if (Guid.TryParse(args[2], out var removeWorkerId))
                {
                    await _workerController.RemoveWorkerAsync(removeWorkerId);
                }
                else
                {
                    Console.WriteLine("Invalid worker ID format");
                }
                break;

            case "status":
                if (args.Length < 3)
                {
                    await _workerController.ShowAllWorkersAsync();
                    return;
                }

                if (Guid.TryParse(args[2], out var statusWorkerId))
                {
                    await _workerController.ShowWorkerStatusAsync(statusWorkerId);
                }
                else
                {
                    Console.WriteLine("Invalid worker ID format");
                }
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
                await _windowsServiceController.StartServiceAsync();
                break;
            case "stop":
                await _windowsServiceController.StopServiceAsync();
                break;
            case "status":
                await _windowsServiceController.GetServiceStatusAsync();
                break;
            default:
                throw new ArgumentException($"Unknown service subcommand: {subCommand}");
        }
    }
}
