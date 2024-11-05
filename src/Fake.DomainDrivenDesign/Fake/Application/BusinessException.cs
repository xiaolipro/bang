using Fake.ExceptionHandling;
using Fake.Logging;
using Microsoft.Extensions.Logging;

namespace Fake.Application;

public class BusinessException(string? message = null, Exception? innerException = null)
    : FakeException(message, innerException), IHasLogLevel, ILocalizeErrorMessage
{
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;
}