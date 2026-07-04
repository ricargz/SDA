using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VulnerableApp.Controllers;
using VulnerableApp.Data;
using VulnerableApp.Models;
using VulnerableApp.Services;

namespace VulnerableApp.Tests;

public sealed class ControllerInstrumentationTests
{
    [Fact]
    public void HomeController_LogsEveryActionAndExceptionScenario()
    {
        var logger = new CapturingLogger<HomeController>();
        var controller = AttachHttpContext(new HomeController(logger));

        controller.Index();
        controller.Privacy();
        Assert.IsType<UnprocessableEntityObjectResult>(controller.ControlledException());
        Assert.Throws<InvalidOperationException>(() => controller.UnhandledException());
        controller.Error();

        AssertActionLifecycle(logger, "Index");
        AssertActionLifecycle(logger, "Privacy");
        AssertActionLifecycle(logger, "ControlledException");
        AssertActionLifecycle(logger, "UnhandledException");
        AssertActionLifecycle(logger, "Error");
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void SearchController_LogsParametersLifecycleAndWarning()
    {
        using var db = CreateDatabase();
        var logger = new CapturingLogger<SearchController>();
        var controller = AttachHttpContext(new SearchController(db, logger));

        controller.Index(string.Empty);
        controller.Index("admin");

        AssertActionLifecycle(logger, "Index");
        Assert.Contains(logger.Entries,
            entry => entry.Message.Contains("Search = admin", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void AuthController_LogsAuthenticationEventsWithoutPassword()
    {
        const string secretPassword = "Secret-Should-Never-Appear!";
        using var db = CreateDatabase();
        db.Users.Add(new User
        {
            Id = 99,
            Username = "logging-user",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(secretPassword),
            Email = "logging-user@test.local",
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();

        var logger = new CapturingLogger<AuthController>();
        var controller = AttachHttpContext(new AuthController(db, logger));

        controller.Login();
        controller.Login("logging-user", "wrong-password");
        controller.Login("logging-user", secretPassword);
        controller.Dashboard();
        controller.Logout();

        AssertActionLifecycle(logger, "Login");
        AssertActionLifecycle(logger, "Dashboard");
        AssertActionLifecycle(logger, "Logout");
        Assert.Contains(logger.Entries,
            entry => entry.Message.Contains("Autenticacion fallida", StringComparison.Ordinal));
        Assert.Contains(logger.Entries,
            entry => entry.Message.Contains("Autenticacion exitosa", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries,
            entry => entry.Message.Contains(secretPassword, StringComparison.Ordinal)
                || entry.Message.Contains("wrong-password", StringComparison.Ordinal));
    }

    [Fact]
    public void CommentController_LogsBothActionsAndSafeParameterMetadata()
    {
        var logger = new CapturingLogger<CommentController>();
        var controller = AttachHttpContext(
            new CommentController(new InMemoryCommentStore(), logger));

        controller.Index();
        controller.AddComment(string.Empty);
        controller.AddComment("Comentario de prueba");

        AssertActionLifecycle(logger, "Index");
        AssertActionLifecycle(logger, "AddComment");
        Assert.Contains(logger.Entries,
            entry => entry.Message.Contains("CommentLength", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void ApiController_LogsAuthorizationBranchesAndBothActions()
    {
        using var db = CreateDatabase();
        var logger = new CapturingLogger<ApiController>();
        var controller = AttachHttpContext(new ApiController(db, logger));

        Assert.IsType<UnauthorizedResult>(controller.GetUser(1));
        controller.HttpContext.Session.SetInt32("UserId", 1);
        Assert.IsType<OkObjectResult>(controller.GetUser(1));
        var forbidden = Assert.IsType<StatusCodeResult>(controller.GetUser(2));
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.IsType<OkObjectResult>(controller.GetAllUsers());

        AssertActionLifecycle(logger, "GetUser");
        AssertActionLifecycle(logger, "GetAllUsers");
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public void UnhandledException_IsLoggedAsErrorAndStillLogsExit()
    {
        var logger = new CapturingLogger<CommentController>();
        var controller = AttachHttpContext(
            new CommentController(new ThrowingCommentStore(), logger));

        Assert.Throws<InvalidOperationException>(() => controller.AddComment("fallara"));

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error);
        Assert.Contains(logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains("Fin CommentController.AddComment", StringComparison.Ordinal)
                && entry.Message.Contains("Excepcion", StringComparison.Ordinal));
    }

    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"controller-tests-{Guid.NewGuid()}")
            .Options;
        var database = new AppDbContext(options);
        database.Database.EnsureCreated();
        return database;
    }

    private static TController AttachHttpContext<TController>(TController controller)
        where TController : Controller
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Features.Set<ISessionFeature>(
            new TestSessionFeature { Session = new TestSession() });
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private static void AssertActionLifecycle<TController>(
        CapturingLogger<TController> logger,
        string actionName)
    {
        var controllerName = typeof(TController).Name;
        Assert.Contains(logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains(
                    $"Inicio {controllerName}.{actionName}",
                    StringComparison.Ordinal));
        Assert.Contains(logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains(
                    $"Fin {controllerName}.{actionName}",
                    StringComparison.Ordinal)
                && entry.Message.Contains("DuracionMs", StringComparison.Ordinal));
    }

    private sealed class ThrowingCommentStore : ICommentStore
    {
        public IReadOnlyCollection<string> GetAll() => [];

        public void Add(string comment) =>
            throw new InvalidOperationException("Fallo controlado para validar logging");
    }
}

internal sealed record CapturedLog(LogLevel Level, string Message, Exception? Exception);

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<CapturedLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new CapturedLog(logLevel, formatter(state, exception), exception));
    }
}

internal sealed class TestSessionFeature : ISessionFeature
{
    public ISession Session { get; set; } = null!;
}

internal sealed class TestSession : ISession
{
    private readonly Dictionary<string, byte[]> _values = new(StringComparer.Ordinal);

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public IEnumerable<string> Keys => _values.Keys;

    public void Clear() => _values.Clear();

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task LoadAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void Remove(string key) => _values.Remove(key);

    public void Set(string key, byte[] value) => _values[key] = value;

    public bool TryGetValue(string key, out byte[] value) =>
        _values.TryGetValue(key, out value!);
}
