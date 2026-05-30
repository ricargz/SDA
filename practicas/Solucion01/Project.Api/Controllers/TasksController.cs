using Microsoft.AspNetCore.Mvc;
using Project.Application.DTOs;
using Project.Application.Interfaces;

namespace Project.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _service;

    public TasksController(ITaskService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _service.GetTasksAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var result = await _service.CreateTaskAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await _service.CompleteTaskAsync(id);
        return NoContent();
    }
}