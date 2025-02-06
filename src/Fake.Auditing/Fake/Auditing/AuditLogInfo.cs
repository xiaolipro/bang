using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fake.Data;

namespace Fake.Auditing;

[Serializable]
public class AuditLogInfo : IHasExtraProperties
{
    public string? ApplicationName { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public DateTime ExecutionTime { get; set; }

    public int ExecutionDuration { get; set; }

    public List<AuditLogActionInfo> Actions { get; set; } = [];

    public List<Exception> Exceptions { get; } = [];

    public List<EntityChangeInfo> EntityChanges { get; set; } = [];

    public ExtraProperties ExtraProperties { get; } = new();

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine("AUDIT LOG:");
        sb.AppendLine($"|- UserName Id: {UserName} {UserId}");
        sb.AppendLine($"|- ExecutionDuration: {ExecutionDuration} ms");

        if (Actions.Any())
        {
            sb.AppendLine("|- Actions:");
            foreach (var action in Actions)
            {
                sb.AppendLine($"  - {action.ServiceName}.{action.MethodName} ({action.ExecutionDuration} ms.)");
                sb.AppendLine($"    {action.Parameters}");
            }
        }

        if (Exceptions.Any())
        {
            sb.AppendLine("|- Exceptions:");
            foreach (var exception in Exceptions)
            {
                sb.AppendLine($"  - {exception.Message}");
                sb.AppendLine($"    {exception}");
            }
        }

        if (EntityChanges.Any())
        {
            sb.AppendLine("|- Entity Changes:");
            foreach (var entityChange in EntityChanges)
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