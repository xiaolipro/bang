using Fake.Application;

namespace Fake.AspNetCore.ExceptionHandling;

public interface IException2ErrorInfoConverter
{
    ApplicationServiceErrorInfo Convert(Exception exception, FakeExceptionHandlingOptions options);
}