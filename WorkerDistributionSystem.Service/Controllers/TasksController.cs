using Microsoft.AspNetCore.Mvc;
using WorkerDistributionSystem.Application.DTOs;
using WorkerDistributionSystem.Application.Exceptions;
using WorkerDistributionSystem.Application.Interfaces.Services;

namespace WorkerDistributionSystem.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpGet]
        public async Task<ActionResult<List<WorkerTaskDto>>> GetAllTasks()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<WorkerTaskDto>> GetTask(Guid id)
        {
            var task = await _taskService.GetTaskByIdAsync(id);
            if (task == null)
                return NotFound($"Task with ID {id} not found");

            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<WorkerTaskDto>> CreateTask(CreateTaskDto createTaskDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var task = await _taskService.CreateTaskAsync(createTaskDto);
            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpGet("pending")]
        public async Task<ActionResult<List<WorkerTaskDto>>> GetPendingTasks()
        {
            var tasks = await _taskService.GetPendingTasksAsync();
            return Ok(tasks);
        }

        [HttpGet("queue/count")]
        public async Task<ActionResult<int>> GetQueueCount()
        {
            var count = await _taskService.GetQueueCountAsync();
            return Ok(count);
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteTask(Guid id, [FromBody] string result)
        {
            try
            {
                var success = await _taskService.CompleteTaskAsync(id, result);
                if (!success)
                    return BadRequest("Failed to complete task");

                return Ok(new { Message = "Task completed successfully" });
            }
            catch (TaskNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost("{id}/fail")]
        public async Task<IActionResult> FailTask(Guid id, [FromBody] string error)
        {
            try
            {
                var success = await _taskService.FailTaskAsync(id, error);
                if (!success)
                    return BadRequest("Failed to fail task");

                return Ok(new { Message = "Task marked as failed" });
            }
            catch (TaskNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("worker/{workerId}")]
        public async Task<ActionResult<List<WorkerTaskDto>>> GetTasksByWorkerId(Guid workerId)
        {
            try
            {
                var tasks = await _taskService.GetTasksByWorkerIdAsync(workerId);
                return Ok(tasks);
            }
            catch (WorkerNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}