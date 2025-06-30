namespace WorkerDistributionSystem.WorkerApp.Services.Interfaces
{
    public class TaskResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
    }

    public interface ICommandExecutor
    {
        Task<TaskResult> ExecuteCommandAsync(string command);
    }
}