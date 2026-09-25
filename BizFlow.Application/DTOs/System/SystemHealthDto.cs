namespace BizFlow.Application.DTOs.System;

public class SystemHealthDto
{
    public string Status { get; set; } = "Healthy";
    public string Environment { get; set; } = string.Empty;
    public DateTimeOffset ServerTime { get; set; } = DateTimeOffset.UtcNow;
    public string Version { get; set; } = "1.0.0";
    public string? DatabaseStatus { get; set; }
    public string? DatabaseProvider { get; set; }
}
