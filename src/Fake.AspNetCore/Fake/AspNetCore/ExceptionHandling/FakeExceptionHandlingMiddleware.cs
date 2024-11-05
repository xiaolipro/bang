using Fake.Authorization;
using Fake.ExceptionHandling;
using Fake.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Fake.AspNetCore.ExceptionHandling;

/// <summary>
/// 异常处理中间件
/// </summary>
public class FakeExceptionHandlingMiddleware(ILogger<FakeExceptionHandlingMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // 如果响应已经开始了，中止该管道
            if (context.Response.HasStarted)
            {
                logger.LogWarning("一个异常发生了，但响应已经开始了！");
                throw; // didn't throw ex
            }

            await HandleAndWrapExceptionAsync(context, ex);
        }
    }

    protected virtual async Task HandleAndWrapExceptionAsync(HttpContext httpContext, Exception exception)
    {
        logger.LogException(exception);

        await httpContext.RequestServices.GetRequiredService<IExceptionNotifier>()
            .NotifyAsync(new ExceptionNotificationContext(exception, httpContext.RequestServices));

        if (exception is FakeAuthorizationException authorizationException)
        {
            await httpContext.RequestServices.GetRequiredService<IAuthorizationExceptionHandler>()
                .HandleAsync(authorizationException, httpContext);
        }
        else
        {
            var statusCodeFinder = httpContext.RequestServices.GetRequiredService<IHttpExceptionStatusCodeFinder>();
            var errorInfoConverter = httpContext.RequestServices.GetRequiredService<IException2ErrorInfoConverter>();
            var exceptionHandlingOptions = httpContext.RequestServices
                .GetRequiredService<IOptions<FakeExceptionHandlingOptions>>().Value;
            var jsonSerializer = httpContext.RequestServices.GetRequiredService<IFakeJsonSerializer>();

            var errorModel = errorInfoConverter.Convert(exception, exceptionHandlingOptions);

            httpContext.Response.Clear();
            httpContext.Response.StatusCode = (int)statusCodeFinder.Find(httpContext, exception);
            httpContext.Response.Headers.Append("Content-Type", "application/json");
            httpContext.Response.OnStarting(ClearCacheHeaders, httpContext.Response);
            await httpContext.Response.WriteAsync(jsonSerializer.Serialize(errorModel));
        }
    }

    static readonly Func<object, Task> ClearCacheHeaders = state =>
    {
        var response = (HttpResponse)state;

        response.Headers[HeaderNames.CacheControl] = "no-cache";
        response.Headers[HeaderNames.Pragma] = "no-cache";
        response.Headers[HeaderNames.Expires] = "-1";
        response.Headers.Remove(HeaderNames.ETag);

        return Task.CompletedTask;
    };
}