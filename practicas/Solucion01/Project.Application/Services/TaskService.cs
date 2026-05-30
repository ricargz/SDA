using Project.Application.DTOs;
using Project.Application.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Interfaces;

namespace Project.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TaskDto>> GetTasksAsync()
    {
        var tasks = await _repository.GetAllAsync();
        return tasks.Select(t => new TaskDto(t.Id, t.Title, t.Description, t.IsCompleted, t.CreatedAt));
    }

    public async Task<TaskDto> CreateTaskAsync(CreateTaskRequest request)
    {
        var task = new TaskItem(request.Title, request.Description);
        await _repository.AddAsync(task);
        return new TaskDto(task.Id, task.Title, task.Description, task.IsCompleted, task.CreatedAt);
    }

    public async Task CompleteTaskAsync(Guid id)
    {
        var task = await _repository.GetByIdAsync(id);
        if (task == null) throw new KeyNotFoundException("Task not found");
        task.MarkAsCompleted();
        await _repository.UpdateAsync(task);
    }
}