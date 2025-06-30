using Microsoft.AspNetCore.Mvc;
using WorkerDistributionSystem.Application.Interfaces.Services;

namespace WorkerDistributionSystem.Service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly IWorkerService _workerService;

        public ServiceController(IWorkerService workerService)
        {
            _workerService = workerService;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetServiceStatus()
        {
            var status = await _workerService.GetServiceStatusAsync();
            return Ok(status);
        }
    }
}