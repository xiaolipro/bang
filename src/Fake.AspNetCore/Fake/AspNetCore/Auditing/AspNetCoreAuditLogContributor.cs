using Fake.AspNetCore.Http;
using Fake.Auditing;
using Fake.Data;
using Microsoft.AspNetCore.Http;

namespace Fake.AspNetCore.Auditing;

public class AspNetCoreAuditLogContributor : AuditLogContributor
{
    public const string RequestSummary = nameof(RequestSummary);
    public const string UserAgent = nameof(UserAgent);
    public const string ClientIpAddress = nameof(ClientIpAddress);
    public const string TraceIdentifier = nameof(TraceIdentifier);

    public override void PreContribute(AuditLogContributionContext context)
    {
        var httpContext = context.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext;

        if (httpContext == null) return;

        if (httpContext.WebSockets.IsWebSocketRequest) return;

        context.AuditInfo.AddExtraProperty(TraceIdentifier, httpContext.TraceIdentifier);
        context.AuditInfo.AddExtraProperty(RequestSummary, BuildSummary(httpContext));

        var httpClientInfoProvider = context.ServiceProvider.GetRequiredService<IHttpClientInfoProvider>();
        context.AuditInfo.AddExtraProperty(ClientIpAddress, httpClientInfoProvider.ClientIpAddress);
        context.AuditInfo.AddExtraProperty(UserAgent, httpClientInfoProvider.UserAgent);
    }

    public override void PostContribute(AuditLogContributionContext context)
    {
    }

    protected virtual string BuildSummary(HttpContext httpContext)
    {
        var request = httpContext.Request;
        return $"{request.Method} {request.Path}{request.QueryString}";
    }
}