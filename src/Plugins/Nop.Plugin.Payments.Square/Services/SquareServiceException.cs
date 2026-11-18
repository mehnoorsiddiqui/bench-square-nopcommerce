using Nop.Core;

namespace Nop.Plugin.Payments.Square.Services;

/// <summary>
/// Represents a failed call to Square: it could not be reached, or it answered with an error or a body that could
/// not be read. Distinct from a valid answer that simply has nothing to return (for example, no active location).
/// Derives from <see cref="NopException"/>, so existing handlers of that type keep working.
/// </summary>
public class SquareServiceException : NopException
{
    /// <summary>
    /// Initializes a new instance of the exception
    /// </summary>
    /// <param name="message">The error message, as logged</param>
    /// <param name="innerException">The exception that made the call fail</param>
    public SquareServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
