using BizFlow.Application.Common.Interfaces;

namespace BizFlow.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateTime UtcDateTime => DateTime.UtcNow;
}
