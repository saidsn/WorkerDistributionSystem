namespace WorkerDistributionSystem.Application.Exceptions
{
    public class WorkerNotFoundException : Exception
    {
        public WorkerNotFoundException(Guid workerId)
            : base($"Worker with ID {workerId} was not found.")
        {
        }
    }
}
