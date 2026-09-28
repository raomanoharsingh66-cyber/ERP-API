using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.DTOs.AiAssistant;

namespace BizFlow.Application.Common.Interfaces;

public interface IAiAssistantService
{
    Task<SmartPurchaseDashboardDto> GetSmartPurchaseDashboardAsync(CancellationToken cancellationToken = default);
    Task<List<SmartReorderRecommendationDto>> GetSmartReordersAsync(CancellationToken cancellationToken = default);
    Task<List<DemandTrendDto>> GetDemandTrendsAsync(CancellationToken cancellationToken = default);
    Task<List<SupplierPerformanceDto>> GetSupplierPerformancesAsync(CancellationToken cancellationToken = default);
    Task<QuotationAnalysisResultDto> AnalyzeQuotationAsync(QuotationAnalysisRequestDto request, CancellationToken cancellationToken = default);
    Task<AiChatResponseDto> AskAssistantAsync(AiChatRequestDto request, CancellationToken cancellationToken = default);
}
