using System.Net;

namespace BizFlow.Application.Common.Exceptions;

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message)
        : base(message, HttpStatusCode.UnprocessableEntity)
    {
    }

    public BusinessRuleException(string message, List<string> errors)
        : base(message, HttpStatusCode.UnprocessableEntity, errors)
    {
    }
}
