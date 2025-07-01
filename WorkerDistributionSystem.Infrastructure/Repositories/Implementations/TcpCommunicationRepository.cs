using System.Net;
using System.Net.Sockets;
using System.Text;
using WorkerDistributionSystem.Infrastructure.Repositories.Interfaces;

namespace WorkerDistributionSystem.Infrastructure.Repositories.Implementations
{
    public class TcpCommunicationRepository : ICommunicationRepository
    {
        private TcpListener? _tcpListener;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private readonly Dictionary<Guid, TcpClient> _workerClients = new Dictionary<Guid, TcpClient>();
        private bool _isRunning = false;
        private readonly object _lock = new object();

        public bool IsRunning => _isRunning;
        public event EventHandler<string>? MessageReceived;

        public async Task StartAsync()
        {
            try
            {
                _tcpListener = new TcpListener(IPAddress.Any, 8080);
                _tcpListener.Start();
                _isRunning = true;

                Console.WriteLine("TCP Server started on port 8080");

                _ = Task.Run(AcceptClientsAsync);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting TCP server: {ex.Message}");
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
            }

            _tcpListener?.Stop();
            Console.WriteLine("TCP Server stopped");

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
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
                }
                Console.WriteLine($"Worker {workerId} registered successfully");
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
                    _clients.Remove(client);
                    Console.WriteLine($"Worker {workerId} disconnected");
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
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.WriteLine($"Error accepting client: {ex.Message}");
                    }
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

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(this, message.Trim());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _clients.Remove(client);
                    var workerToRemove = _workerClients.FirstOrDefault(w => w.Value == client);
                    if (workerToRemove.Key != Guid.Empty)
                    {
                        _workerClients.Remove(workerToRemove.Key);
                    }
                }
                client.Close();
            }
        }
    }
}