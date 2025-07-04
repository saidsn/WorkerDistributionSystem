﻿using System.Net.Sockets;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Interfaces
{
	public interface ICommunicationRepository
	{
        Task StartAsync();
        Task StopAsync();
        Task<bool> SendMessageAsync(Guid recipientId, string message);
        event EventHandler<(string message, TcpClient client)>? MessageReceivedWithClient;
        bool IsRunning { get; }
        void RegisterWorker(Guid workerId, object connection);    
        Task<bool> IsWorkerConnectedAsync(Guid workerId);        
        Task DisconnectWorkerAsync(Guid workerId);               
        Task<int> GetConnectedWorkerCountAsync();
    }
}

