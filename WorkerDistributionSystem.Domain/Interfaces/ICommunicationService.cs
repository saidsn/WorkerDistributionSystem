namespace WorkerDistributionSystem.Domain.Interfaces
{
	public interface ICommunicationService
	{
        Task StartAsync();
        Task StopAsync();
        Task<bool> SendMessageAsync(Guid recipientId, string message);
        event EventHandler<string> MessageReceived;
        bool IsRunning { get; }
    }
}

