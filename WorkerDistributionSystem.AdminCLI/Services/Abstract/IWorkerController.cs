namespace WorkerDistributionSystem.AdminCLI.Services.Abstract
{
    public interface IWorkerController
    {
        Task ExecuteCommandAsync(string command);
        Task AddWorkerAsync();
        Task RemoveWorkerAsync();
        Task<string> GetWorkerStatusAsync();
    }
}
