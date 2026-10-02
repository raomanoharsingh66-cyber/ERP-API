using System;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services.AiSupport;

public class AiAuditService : IAiAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AiAuditService> _logger;

    public AiAuditService(IApplicationDbContext context, ILogger<AiAuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<long> LogAiActionAsync(
        Guid userId,
        Guid businessId,
        string sessionId,
        string userRequest,
        string actionType,
        string payloadJson,
        bool userConfirmed,
        bool success,
        string resultMessage,
        string? recordId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var audit = new AuditLog
            {
                UserId = userId,
                BusinessId = businessId,
                Action = $"AI_ACTION: {actionType} (Confirmed: {userConfirmed}, Success: {success})",
                Entity = "AI_COPILOT",
                EntityId = recordId ?? sessionId,
                OldValue = $"Request: {userRequest} | Session: {sessionId} | Payload: {payloadJson}",
                NewValue = $"Result: {resultMessage}",
                IpAddress = "AI_ASSISTANT_INTERNAL",
                Timestamp = DateTimeOffset.UtcNow
            };

            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync(cancellationToken);
            return audit.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log AI Action audit entry: {ActionType}", actionType);
            return 0;
        }
    }
}
