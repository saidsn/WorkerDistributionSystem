using System.Net;
using System.Net.Sockets;
using System.Text;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Implementations
{
    public class TcpCommunicationRepository : ICommunicationRepository
    {
        private TcpListener? _tcpListener;
        private readonly List<TcpClient> _clients = new();
        private readonly Dictionary<Guid, TcpClient> _workerClients = new();
        private readonly Dictionary<TcpClient, Guid> _clientWorkerMap = new();
        private bool _isRunning = false;
        private readonly object _lock = new();

        public bool IsRunning => _isRunning;

        public event EventHandler<string>? MessageReceived;
        public event EventHandler<(string message, TcpClient client)>? MessageReceivedWithClient;

        public async Task StartAsync()
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, 8080);
                _tcpListener.Start();
                _isRunning = true;

                _ = Task.Run(AcceptClientsAsync);
            }
            catch
            {
                throw;
            }

            await Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            _isRunning = false;

            lock (_lock)
            {
                foreach (var client in _clients.ToList())
                {
                    client?.Close();
                }

                _clients.Clear();
                _workerClients.Clear();
                _clientWorkerMap.Clear();
            }

            _tcpListener?.Stop();

            await Task.CompletedTask;
        }

        public async Task<bool> SendMessageAsync(Guid recipientId, string message)
        {
            try
            {
                lock (_lock)
                {
                    if (_workerClients.TryGetValue(recipientId, out var client))
                    {
                        var data = Encoding.UTF8.GetBytes(message + "\n");
                        var stream = client.GetStream();
                        stream.Write(data, 0, data.Length);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return await Task.FromResult(false);
        }

        public void RegisterWorker(Guid workerId, object connection)
        {
            if (connection is TcpClient tcpClient)
            {
                lock (_lock)
                {
                    _workerClients[workerId] = tcpClient;
                    _clientWorkerMap[tcpClient] = workerId;
                }
            }
            else
            {
                throw new ArgumentException("Connection must be TcpClient", nameof(connection));
            }
        }

        public Task<bool> IsWorkerConnectedAsync(Guid workerId)
        {
            bool isConnected;

            lock (_lock)
            {
                isConnected = _workerClients.ContainsKey(workerId) &&
                              _workerClients[workerId].Connected;
            }

            return Task.FromResult(isConnected);
        }

        public async Task DisconnectWorkerAsync(Guid workerId)
        {
            lock (_lock)
            {
                if (_workerClients.TryGetValue(workerId, out var client))
                {
                    client.Close();
                    _workerClients.Remove(workerId);
                    _clientWorkerMap.Remove(client);
                    _clients.Remove(client);
                }
            }

            await Task.CompletedTask;
        }

        public Task<int> GetConnectedWorkerCountAsync()
        {
            int count;

            lock (_lock)
            {
                count = _workerClients.Count(kvp => kvp.Value.Connected);
            }

            return Task.FromResult(count);
        }

        private async Task AcceptClientsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    if (_tcpListener != null)
                    {
                        var tcpClient = await _tcpListener.AcceptTcpClientAsync();

                        lock (_lock)
                        {
                            _clients.Add(tcpClient);
                        }

                        _ = Task.Run(() => HandleClientAsync(tcpClient));
                    }
                }
                catch
                {
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];

                while (_isRunning && client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    MessageReceived?.Invoke(this, message);
                    MessageReceivedWithClient?.Invoke(this, (message, client));
                }
            }
            catch
            {
            }
            finally
            {
                lock (_lock)
                {
                    _clients.Remove(client);

                    if (_clientWorkerMap.TryGetValue(client, out var workerId))
                    {
                        _workerClients.Remove(workerId);
                        _clientWorkerMap.Remove(client);
                    }
                }

                client.Close();
            }
        }
    }
}
