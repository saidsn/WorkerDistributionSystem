namespace WorkerDistributionSystem.AdminCLI.Commands
{
    public interface ICommandProcessor
    {
        Task ProcessAsync(string[] args);
    }
}
