namespace Project.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TaskItem() { } // Para EF Core

    public TaskItem(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsCompleted() => IsCompleted = true;
}