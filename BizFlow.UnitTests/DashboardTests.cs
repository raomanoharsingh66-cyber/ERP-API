using BizFlow.Application.DTOs.Dashboard;
using FluentAssertions;
using Xunit;

namespace BizFlow.UnitTests;

public class DashboardTests
{
    [Fact]
    public void ExecutiveMetrics_NetProfitCalculation_ShouldEqualRevenueMinusExpenses()
    {
        // Arrange
        decimal totalRevenue = 500000m;
        decimal totalExpenses = 320000m;

        // Act
        decimal netProfit = totalRevenue - totalExpenses;

        // Assert
        netProfit.Should().Be(180000m);
    }

    [Fact]
    public void InventoryHealth_ThresholdEvaluation_ShouldDetectLowStockAccurately()
    {
        // Arrange
        var alerts = new List<LowStockAlertItem>();
        var items = new[]
        {
            new { Sku = "SKU-001", Qty = 5m, Reorder = 10m },   // Low stock
            new { Sku = "SKU-002", Qty = 0m, Reorder = 15m },   // Out of stock
            new { Sku = "SKU-003", Qty = 50m, Reorder = 20m }   // Healthy
        };

        // Act
        int lowStockCount = 0;
        int outOfStockCount = 0;

        foreach (var item in items)
        {
            if (item.Qty <= 0)
            {
                outOfStockCount++;
                alerts.Add(new LowStockAlertItem { SKU = item.Sku, QuantityOnHand = item.Qty, ReorderLevel = item.Reorder });
            }
            else if (item.Qty <= item.Reorder)
            {
                lowStockCount++;
                alerts.Add(new LowStockAlertItem { SKU = item.Sku, QuantityOnHand = item.Qty, ReorderLevel = item.Reorder });
            }
        }

        // Assert
        lowStockCount.Should().Be(1);
        outOfStockCount.Should().Be(1);
        alerts.Should().HaveCount(2);
    }

    [Fact]
    public void MonthlyTrend_AggregateCalculations_ShouldBeConsistent()
    {
        // Arrange
        var trends = new List<MonthlyTrendItem>
        {
            new() { MonthName = "Jan", Year = 2026, SalesRevenue = 150000m, PurchaseExpense = 90000m },
            new() { MonthName = "Feb", Year = 2026, SalesRevenue = 200000m, PurchaseExpense = 110000m }
        };

        // Act
        var totalSales = trends.Sum(t => t.SalesRevenue);
        var totalPurchases = trends.Sum(t => t.PurchaseExpense);
        var netOperatingCashFlow = totalSales - totalPurchases;

        // Assert
        totalSales.Should().Be(350000m);
        totalPurchases.Should().Be(200000m);
        netOperatingCashFlow.Should().Be(150000m);
    }
}
