using System.Net;

namespace BizFlow.Application.Common.Exceptions;

public class ValidationException : AppException
{
    public IDictionary<string, string[]> Failures { get; }

    public ValidationException()
        : base("One or more validation failures have occurred.", HttpStatusCode.BadRequest)
    {
        Failures = new Dictionary<string, string[]>();
    }

    public ValidationException(List<string> errors)
        : base("Validation failed.", HttpStatusCode.BadRequest, errors)
    {
        Failures = new Dictionary<string, string[]>
        {
            { "General", errors.ToArray() }
        };
    }

    public ValidationException(IDictionary<string, string[]> failures)
        : base("One or more validation failures have occurred.", HttpStatusCode.BadRequest, failures.Values.SelectMany(x => x).ToList())
    {
        Failures = failures;
    }
}
