using Microsoft.AspNetCore.Mvc;
using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Exceptions;
using WorkerDistributionSystem.Application.Interfaces.Services;
using WorkerDistributionSystem.Application.Services;

namespace WorkerDistributionSystem.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WorkersController : ControllerBase
    {
        private readonly IWorkerService _workerService;
        private readonly ITaskService _taskService;

        public WorkersController(IWorkerService workerService, ITaskService taskService)
        {
            _workerService = workerService;
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<List<WorkerDto>>> GetAllWorkers()
        {
            var workers = await _workerService.GetAllWorkersAsync();
            return Ok(workers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WorkerDto>> GetWorker(Guid id)
        {
            try
            {
                var worker = await _workerService.GetWorkerAsync(id);
                if (worker == null)
                    return NotFound($"Worker with ID {id} not found");

                return Ok(worker);
            }
            catch (WorkerNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<WorkerDto>> RegisterWorker(CreateWorkerDto createWorkerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var worker = await _workerService.RegisterWorkerAsync(createWorkerDto);
            return CreatedAtAction(nameof(GetWorker), new { id = worker.Id }, worker);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> UnregisterWorker(Guid id)
        {
            try
            {
                var result = await _workerService.UnregisterWorkerAsync(id);
                if (!result)
                    return NotFound($"Worker with ID {id} not found");

                return NoContent();
            }
            catch (WorkerNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id}/heartbeat")]
        public async Task<IActionResult> ProcessHeartbeat(Guid id)
        {
            var result = await _workerService.ProcessHeartbeatAsync(id);
            if (!result)
                return NotFound($"Worker with ID {id} not found");

            return Ok(new { Message = "Heartbeat processed successfully" });
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateWorkerStatus(Guid id, [FromBody] bool isActive)
        {
            var result = await _workerService.UpdateWorkerStatusAsync(id, isActive);
            if (!result)
                return NotFound($"Worker with ID {id} not found");

            return Ok(new { Message = "Worker status updated successfully" });
        }

        [HttpGet("{id}/task")]
        public async Task<ActionResult<WorkerTaskDto>> GetTaskForWorker(Guid id)
        {
            try
            {
                var task = await _workerService.GetTaskForWorkerAsync(id);
                if (task == null)
                    return Ok(new { Message = "No tasks available" });

                return Ok(task);
            }
            catch (WorkerNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("{id}/tasks")]
        public async Task<ActionResult<List<WorkerTaskDto>>> GetWorkerTasks(Guid id)
        {
            try
            {
                var tasks = await _taskService.GetTasksByWorkerIdAsync(id);
                return Ok(tasks);
            }
            catch (WorkerNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}