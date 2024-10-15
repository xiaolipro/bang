using System.Text;
using Fake.Auditing;
using Fake.Data;
using Microsoft.Extensions.Logging;

namespace Fake.AspNetCore.Auditing;

public class AspNetCoreAuditingStore(ILogger<AspNetCoreAuditingStore> logger) : IAuditingStore
{
    public virtual Task SaveAsync(AuditLogInfo auditInfo)
    {
        logger.LogInformation("{Log}", Build(auditInfo));
        return Task.CompletedTask;
    }

    protected virtual string Build(AuditLogInfo auditInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"AUDIT LOG: {auditInfo.GetExtraProperty(AspNetCoreAuditLogContributor.RequestSummary)}");
        sb.AppendLine(
            $"|- TraceIdentifier: {auditInfo.GetExtraProperty(AspNetCoreAuditLogContributor.TraceIdentifier)}");
        sb.AppendLine($"|- UserName-Id: {auditInfo.UserName} - {auditInfo.UserId}");
        sb.AppendLine(
            $"|- ClientIpAddress: {auditInfo.GetExtraProperty(AspNetCoreAuditLogContributor.ClientIpAddress)}");
        sb.AppendLine(
            $"|- UserUserAgent: {auditInfo.GetExtraProperty(AspNetCoreAuditLogContributor.UserAgent)}");
        sb.AppendLine($"|- ExecutionDuration: {auditInfo.ExecutionDuration} ms");

        if (auditInfo.Actions.Count != 0)
        {
            sb.AppendLine("|- Actions:");
            foreach (var action in auditInfo.Actions)
            {
                sb.AppendLine($"  - {action.ServiceName}.{action.MethodName} ({action.ExecutionDuration} ms.)");
                sb.AppendLine($"    {action.Parameters}");
            }
        }

        if (auditInfo.Exceptions.Count != 0)
        {
            sb.AppendLine("|- Exceptions:");
            foreach (var exception in auditInfo.Exceptions)
            {
                sb.AppendLine($"  - {exception.Message}");
                sb.AppendLine($"    {exception}");
            }
        }

        if (auditInfo.EntityChanges.Count != 0)
        {
            sb.AppendLine("|- Entity Changes:");
            foreach (var entityChange in auditInfo.EntityChanges)
            {
                sb.AppendLine(
                    $"  - [{entityChange.ChangeType}] {entityChange.EntityTypeFullName}, Id = {entityChange.EntityId}");
                foreach (var propertyChange in entityChange.PropertyChanges)
                {
                    sb.AppendLine(
                        $"    {propertyChange.PropertyName}: {propertyChange.OriginalValue} -> {propertyChange.NewValue}");
                }
            }
        }

        return sb.ToString();
    }
}