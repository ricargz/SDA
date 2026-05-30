using Project.Application.DTOs;

namespace Project.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetTasksAsync();
    Task<TaskDto> CreateTaskAsync(CreateTaskRequest request);
    Task CompleteTaskAsync(Guid id);
}