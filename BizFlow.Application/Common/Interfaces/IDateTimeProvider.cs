namespace BizFlow.Application.Common.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
    DateTime UtcDateTime => UtcNow.UtcDateTime;
}
