using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Dashboard;

namespace BizFlow.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetExecutiveDashboardAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        string? search = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
