namespace BizFlow.Application.DTOs.Dashboard;

public class MonthlyTrendItem
{
    public string MonthName { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal SalesRevenue { get; set; }
    public decimal PurchaseExpense { get; set; }
}

public class RecentActivityItem
{
    public string ActivityType { get; set; } = string.Empty; // "Invoice", "PurchaseOrder", "JournalVoucher", "Stock"
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string StatusBadge { get; set; } = string.Empty;
    public string ReferenceId { get; set; } = string.Empty;
}

public class LowStockAlertItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal QuantityOnHand { get; set; }
    public decimal ReorderLevel { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
}

public class DashboardMetricsDto
{
    // Executive Financial KPIs
    public decimal TotalRevenue { get; set; }
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public decimal InventoryValuation { get; set; }
    public decimal CashBankBalance { get; set; }
    public decimal NetProfitYTD { get; set; }

    // Inventory Health
    public int TotalProducts { get; set; }
    public decimal TotalStockQuantity { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }

    // Multi-Month Trends (last 6 months)
    public List<MonthlyTrendItem> MonthlyTrends { get; set; } = new();

    // Cross-Module Activity Feed
    public List<RecentActivityItem> RecentActivities { get; set; } = new();

    // Critical Reorder Level Warnings
    public List<LowStockAlertItem> LowStockAlerts { get; set; } = new();
}
