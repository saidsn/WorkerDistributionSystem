namespace WorkerDistributionSystem.AdminCLI.Services.Abstract
{
    public interface IServiceController
    {
        Task StartServiceAsync();
        Task StopServiceAsync();
        Task<string> GetServiceStatusAsync();
    }
}
