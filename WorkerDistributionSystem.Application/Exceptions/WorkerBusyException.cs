namespace WorkerDistributionSystem.Application.Exceptions
{
    public class WorkerBusyException : Exception
    {
        public WorkerBusyException(Guid workerId)
            : base($"Worker with ID {workerId} is currently busy.")
        {
        }
    }
}