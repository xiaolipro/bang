using System;
using System.Runtime.Serialization;

namespace Fake.Authorization;

[Serializable]
public class FakeAuthorizationException : FakeException
{
    public FakeAuthorizationException()
    {
    }

    public FakeAuthorizationException(string message)
        : base(message)
    {
    }
    

    public FakeAuthorizationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public FakeAuthorizationException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {
    }
}