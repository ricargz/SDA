using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using VulnerableApp.Middleware;
using VulnerableApp.Security;

namespace VulnerableApp.Tests;

public sealed class MiddlewareAndSecurityTests
{
    [Fact]
    public async Task CorrelationIdMiddleware_GeneratesHeaderAndPropagatesIdentifier()
    {
        var context = CreateHttpContext();
        string? observedCorrelationId = null;
        var middleware = new CorrelationIdMiddleware(async httpContext =>
        {
            observedCorrelationId = httpContext.TraceIdentifier;
            await httpContext.Response.WriteAsync("ok");
        });

        await middleware.InvokeAsync(context);

        var responseCorrelationId =
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.NotEmpty(responseCorrelationId);
        Assert.Equal(responseCorrelationId, observedCorrelationId);
        Assert.Equal(responseCorrelationId, context.TraceIdentifier);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_UsesSafeIncomingIdentifier()
    {
        var context = CreateHttpContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "p3g-request-001";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("p3g-request-001", context.TraceIdentifier);
    }

    [Fact]
    public async Task ExceptionMiddleware_ReturnsProblemAndLogsUnhandledException()
    {
        var logger = new CapturingLogger<ExceptionLoggingMiddleware>();
        var context = CreateHttpContext();
        context.TraceIdentifier = "exception-correlation";
        var middleware = new ExceptionLoggingMiddleware(
            _ => throw new InvalidOperationException("Fallo de prueba"),
            logger);

        await middleware.InvokeAsync(context);
        context.Response.Body.Position = 0;
        var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(
            "exception-correlation",
            context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString());
        Assert.Contains("exception-correlation", responseBody, StringComparison.Ordinal);
        Assert.Contains(logger.Entries,
            entry => entry.Level == LogLevel.Error
                && entry.Exception is InvalidOperationException);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_LogsMethodPathStatusTimeAndCorrelation()
    {
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var context = CreateHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/prueba";
        context.TraceIdentifier = "request-correlation";
        var middleware = new RequestLoggingMiddleware(
            httpContext =>
            {
                httpContext.Response.StatusCode = StatusCodes.Status202Accepted;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        Assert.Contains(logger.Entries,
            entry => entry.Level == LogLevel.Information
                && entry.Message.Contains("HTTP POST /prueba", StringComparison.Ordinal)
                && entry.Message.Contains("202", StringComparison.Ordinal)
                && entry.Message.Contains("request-correlation", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("admin' UNION SELECT password FROM users--")]
    [InlineData("1; DROP TABLE Users")]
    public void SecurityDetector_RecognizesSqlInjection(string value)
    {
        Assert.True(SecurityPatternDetector.LooksLikeSqlInjection(value));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("javascript:alert(document.cookie)")]
    public void SecurityDetector_RecognizesXss(string value)
    {
        Assert.True(SecurityPatternDetector.LooksLikeXss(value));
    }

    [Fact]
    public void SecurityDetector_SanitizesControlCharactersAndLength()
    {
        var value = $"linea1\r\nlinea2\t{new string('x', 250)}";

        var result = SecurityPatternDetector.SanitizeForLog(value, 40);

        Assert.NotNull(result);
        Assert.DoesNotContain('\r', result);
        Assert.DoesNotContain('\n', result);
        Assert.DoesNotContain('\t', result);
        Assert.Equal(40, result.Length);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Response.Body = new MemoryStream();
        context.Features.Set<ISessionFeature>(
            new TestSessionFeature { Session = new TestSession() });
        return context;
    }
}
