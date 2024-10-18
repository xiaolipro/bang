using System.Text;
using Fake.Application;

namespace Fake.AspNetCore.ExceptionHandling;

public interface IExceptionToErrorInfoConverter
{
    ApplicationServiceErrorInfo Convert(Exception exception, FakeExceptionHandlingOptions options);
}

public class DefaultExceptionToErrorInfoConverter : IExceptionToErrorInfoConverter
{
    public virtual ApplicationServiceErrorInfo Convert(Exception exception, FakeExceptionHandlingOptions options)
    {
        var errorInfo = new ApplicationServiceErrorInfo
        {
            Message = exception.Message
        };

        LocalizeFakeException(exception, errorInfo);

        if (options.OutputStackTrace)
        {
            var stackTrace = new StringBuilder();
            AddExceptionToDetails(exception, stackTrace);
            errorInfo.Details = stackTrace.ToString();
        }

        return errorInfo;
    }

    private void 
        LocalizeFakeException(Exception exception, ApplicationServiceErrorInfo errorInfo)
    {
        throw new NotImplementedException();
    }

    protected virtual void AddExceptionToDetails(Exception exception, StringBuilder stackTrace)
    {
        // message
        stackTrace.AppendLine(exception.GetType().Name + ": " + exception.Message);

        // stack trace
        stackTrace.AppendLine($"Stack Trace: {exception.StackTrace}");

        // inner exception
        if (exception.InnerException != null)
        {
            AddExceptionToDetails(exception.InnerException, stackTrace);
        }

        // aggregate exception
        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                AddExceptionToDetails(innerException, stackTrace);
            }
        }
    }
}