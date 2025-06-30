using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.WorkerApp.Services.Interfaces;

namespace WorkerDistributionSystem.Worker.Services
{
    public class WorkerApiService : IWorkerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<WorkerApiService> _logger;

        public WorkerApiService(HttpClient httpClient, ILogger<WorkerApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _baseUrl = "http://localhost:5000/api";

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WorkerApp/1.0");
        }

        public async Task<WorkerDto?> RegisterWorkerAsync(string workerName)
        {
            try
            {
                var request = new CreateWorkerDto
                {
                    Name = workerName,
                    ProcessId = Environment.ProcessId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/workers/register", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var worker = JsonSerializer.Deserialize<WorkerDto>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogInformation("Worker registered successfully: {WorkerId}", worker?.Id);
                    return worker;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to register worker: {StatusCode} - {Error}",
                        response.StatusCode, error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering worker");
                return null;
            }
        }

        public async Task<bool> SendHeartbeatAsync(Guid workerId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/workers/{workerId}/heartbeat", null);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Heartbeat sent successfully for worker {WorkerId}", workerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to send heartbeat for worker {WorkerId}: {StatusCode}",
                        workerId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat for worker {WorkerId}", workerId);
                return false;
            }
        }

        public async Task<WorkerTaskDto?> GetTaskAsync(Guid workerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/workers/{workerId}/task");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (content.Contains("No tasks available"))
                    {
                        return null;
                    }

                    var task = JsonSerializer.Deserialize<WorkerTaskDto>(content,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (task?.Id != Guid.Empty)
                    {
                        _logger.LogInformation("Received task {TaskId}: {Command}", task.Id, task.Command);
                        return task;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task for worker {WorkerId}", workerId);
                return null;
            }
        }

        public async Task<bool> CompleteTaskAsync(Guid taskId, string result)
        {
            try
            {
                var content = new StringContent($"\"{result}\"", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/tasks/{taskId}/complete", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Task {TaskId} completed successfully", taskId);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to complete task {TaskId}: {StatusCode} - {Error}",
                        taskId, response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> FailTaskAsync(Guid taskId, string error)
        {
            try
            {
                var content = new StringContent($"\"{error}\"", Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/tasks/{taskId}/fail", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Task {TaskId} marked as failed", taskId);
                    return true;
                }
                else
                {
                    var responseError = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to fail task {TaskId}: {StatusCode} - {Error}",
                        taskId, response.StatusCode, responseError);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error failing task {TaskId}", taskId);
                return false;
            }
        }

        public async Task<bool> UnregisterWorkerAsync(Guid workerId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_baseUrl}/workers/{workerId}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Worker {WorkerId} unregistered successfully", workerId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to unregister worker {WorkerId}: {StatusCode}",
                        workerId, response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering worker {WorkerId}", workerId);
                return false;
            }
        }
    }
}