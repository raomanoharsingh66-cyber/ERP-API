using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.ClothHub;

public class ClothSalesReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public Guid? ClothSalesInvoiceId { get; set; }
    public string? OriginalInvoiceNumber { get; set; }

    public string CustomerName { get; set; } = "Walk-in Customer";
    public string? CustomerPhone { get; set; }

    public string ReturnType { get; set; } = "Exchange"; // "Exchange" or "Return"
    public decimal TotalReturnAmount { get; set; }
    public decimal TotalExchangeAmount { get; set; }
    public decimal NetDifference { get; set; }

    public string SettlementMode { get; set; } = "Even";
    public string? CreditNoteNumber { get; set; }
    public string Reason { get; set; } = "Size Issue";
    public string? Remarks { get; set; }
    public string? HandledBy { get; set; }

    public List<ClothSalesReturnItemDto> Items { get; set; } = new();
}

public class ClothSalesReturnItemDto
{
    public Guid Id { get; set; }
    public string ItemAction { get; set; } = "Return"; // "Return" or "Replacement"
    public Guid ClothProductVariantId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string ColourName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public string Condition { get; set; } = "Fresh / Resaleable";
}

public class CreateClothSalesReturnDto
{
    public Guid? ClothSalesInvoiceId { get; set; }
    public string? OriginalInvoiceNumber { get; set; }
    public string CustomerName { get; set; } = "Walk-in Customer";
    public string? CustomerPhone { get; set; }

    public string ReturnType { get; set; } = "Exchange"; // "Exchange" or "Return"
    public string Reason { get; set; } = "Size Issue";
    public string? Remarks { get; set; }
    public string SettlementMode { get; set; } = "Even"; // "Even", "Cash", "UPI", "CreditNote", "Card"

    // Items customer is returning
    public List<CreateClothReturnItemInputDto> ReturnItems { get; set; } = new();

    // Replacement items customer is taking (if Exchange)
    public List<CreateClothReturnItemInputDto> ExchangeItems { get; set; } = new();
}

public class CreateClothReturnItemInputDto
{
    public Guid ClothProductVariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string Condition { get; set; } = "Fresh / Resaleable"; // "Fresh / Resaleable" or "Damaged / Defective"
}

public class ClothSalesReturnSummaryDto
{
    public int TotalReturnsCount { get; set; }
    public int TotalExchangesCount { get; set; }
    public decimal TotalReturnedAmount { get; set; }
    public decimal TotalCreditNotesIssued { get; set; }
    public int TodayReturnsCount { get; set; }
    public int TodayExchangesCount { get; set; }
}
