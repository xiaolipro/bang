using System.Text;
using Fake.Application;
using Fake.ExceptionHandling;
using Fake.Localization;
using Microsoft.Extensions.Localization;

namespace Fake.AspNetCore.ExceptionHandling;

public class DefaultException2ErrorInfoConverter : IException2ErrorInfoConverter
{
    private readonly IStringLocalizerFactory _stringLocalizerFactory;
    private readonly IOptions<FakeLocalizationOptions> _localizationOptions;

    public DefaultException2ErrorInfoConverter(IStringLocalizerFactory stringLocalizerFactory, IOptions<FakeLocalizationOptions> localizationOptions)
    {
        _stringLocalizerFactory = stringLocalizerFactory;
        _localizationOptions = localizationOptions;
    }

    public virtual ApplicationServiceErrorInfo Convert(Exception exception, FakeExceptionHandlingOptions options)
    {
        var errorInfo = new ApplicationServiceErrorInfo(exception.Message);

        LocalizeErrorMessage(exception, errorInfo);

        if (options.OutputStackTrace)
        {
            var stackTrace = new StringBuilder();
            AddExceptionToDetails(exception, stackTrace);
            errorInfo.Details = stackTrace.ToString();
        }

        return errorInfo;
    }

    private void LocalizeErrorMessage(Exception exception, ApplicationServiceErrorInfo errorInfo)
    {
        if (exception is not ILocalizeErrorMessage) return;

        var errorResourceType = _localizationOptions.Value.DefaultErrorResourceType;
        if (errorResourceType == null)  return;
        
        var stringLocalizer = _stringLocalizerFactory.Create(errorResourceType);
        errorInfo.Message = stringLocalizer[exception.Message];
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