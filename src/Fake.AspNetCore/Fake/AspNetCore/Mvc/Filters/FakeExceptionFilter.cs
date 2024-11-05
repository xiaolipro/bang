using Fake.AspNetCore.ExceptionHandling;
using Fake.Authorization;
using Fake.ExceptionHandling;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Fake.AspNetCore.Mvc.Filters;

public class FakeExceptionFilter(ILogger<FakeExceptionFilter> logger) : IAsyncExceptionFilter
{
    public virtual async Task OnExceptionAsync(ExceptionContext context)
    {
        if (!ShouldHandle(context)) return;

        await HandleAndWrapExceptionAsync(context);

        context.ExceptionHandled = true; // Handled!
    }

    private bool ShouldHandle(ExceptionContext context)
    {
        if (context.ExceptionHandled) return false;

        if (context.ActionDescriptor is ControllerActionDescriptor)
        {
            return true;
        }

        return false;
    }

    protected virtual async Task HandleAndWrapExceptionAsync(ExceptionContext context)
    {
        logger.LogException(context.Exception);

        var httpContext = context.HttpContext;
        await httpContext.RequestServices.GetRequiredService<IExceptionNotifier>()
            .NotifyAsync(new ExceptionNotificationContext(context.Exception, httpContext.RequestServices));

        if (context.Exception is FakeAuthorizationException authorizationException)
        {
            await httpContext.RequestServices.GetRequiredService<IAuthorizationExceptionHandler>()
                .HandleAsync(authorizationException, httpContext);
        }
        else
        {
            var statusCodeFinder = httpContext.RequestServices.GetRequiredService<IHttpExceptionStatusCodeFinder>();
            var exceptionHandlingOptions = httpContext.RequestServices
                .GetRequiredService<IOptions<FakeExceptionHandlingOptions>>()
                .Value;
            var exceptionToErrorInfoConverter = httpContext.RequestServices
                .GetRequiredService<IException2ErrorInfoConverter>();
            var remoteServiceErrorInfo =
                exceptionToErrorInfoConverter.Convert(context.Exception, exceptionHandlingOptions);

            context.HttpContext.Response.StatusCode = (int)statusCodeFinder.Find(httpContext, context.Exception);
            context.Result = new ObjectResult(remoteServiceErrorInfo);
        }
    }
}