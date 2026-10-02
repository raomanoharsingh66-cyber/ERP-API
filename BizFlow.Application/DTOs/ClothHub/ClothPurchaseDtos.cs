using System;
using System.Collections.Generic;

namespace BizFlow.Application.DTOs.ClothHub;

// --- SUPPLIER DTOs ---

public class ClothSupplierDto
{
    public Guid Id { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gstin { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string PaymentTerms { get; set; } = "Net 30 Days";
    public decimal CreditLimit { get; set; }
    public decimal CurrentPayableBalance { get; set; }
    public bool IsActive { get; set; }
    public int BillsCount { get; set; }
}

public class CreateClothSupplierDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gstin { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string PaymentTerms { get; set; } = "Net 30 Days";
    public decimal CreditLimit { get; set; } = 500000;
}

// --- PURCHASE BILL DTOs ---

public class ClothPurchaseBillDto
{
    public Guid Id { get; set; }
    public Guid ClothSupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierGstin { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal RoundOff { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public string InwardStatus { get; set; } = "Received";
    public string? Notes { get; set; }
    public int TotalPieces { get; set; }
    public List<ClothPurchaseBillItemDto> Items { get; set; } = new();
}

public class ClothPurchaseBillItemDto
{
    public Guid Id { get; set; }
    public string ItemType { get; set; } = "VariantLoose"; // VariantLoose or BoxPack
    public Guid? ClothProductVariantId { get; set; }
    public string? VariantSku { get; set; }
    public string? SizeName { get; set; }
    public string? ColourName { get; set; }
    public Guid? ClothBoxPackId { get; set; }
    public string? BoxPackCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal GstRate { get; set; }
    public decimal GstAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class CreateClothPurchaseBillDto
{
    public Guid ClothSupplierId { get; set; }
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public DateTime BillDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string InwardStatus { get; set; } = "Received"; // "Received" immediately posts to inventory stock!
    public decimal DiscountAmount { get; set; } = 0;
    public decimal PaidAmount { get; set; } = 0;
    public string? Notes { get; set; }
    public List<CreateClothPurchaseBillItemDto> Items { get; set; } = new();
}

public class CreateClothPurchaseBillItemDto
{
    public string ItemType { get; set; } = "VariantLoose"; // "VariantLoose" or "BoxPack"
    public Guid? ClothProductVariantId { get; set; }
    public Guid? ClothBoxPackId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitRate { get; set; }
    public decimal GstRate { get; set; } = 5.0m;
    public decimal DiscountAmount { get; set; } = 0;
}

public class RecordSupplierPaymentDto
{
    public decimal Amount { get; set; }
    public string PaymentMode { get; set; } = "Bank Transfer"; // Bank Transfer, UPI, Cheque, Cash
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
}

// --- PURCHASE RETURN DTOs ---

public class ClothPurchaseReturnDto
{
    public Guid Id { get; set; }
    public Guid ClothSupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid? ClothPurchaseBillId { get; set; }
    public string? BillNumber { get; set; }
    public string DebitNoteNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalReturnAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public List<ClothPurchaseReturnItemDto> Items { get; set; } = new();
}

public class ClothPurchaseReturnItemDto
{
    public Guid Id { get; set; }
    public Guid? ClothProductVariantId { get; set; }
    public string? VariantSku { get; set; }
    public Guid? ClothBoxPackId { get; set; }
    public string? BoxPackCode { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitRate { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CreateClothPurchaseReturnDto
{
    public Guid ClothSupplierId { get; set; }
    public Guid? ClothPurchaseBillId { get; set; }
    public string Reason { get; set; } = "Damaged / Defective Garment";
    public string? Remarks { get; set; }
    public List<CreateClothPurchaseReturnItemDto> Items { get; set; } = new();
}

public class CreateClothPurchaseReturnItemDto
{
    public Guid? ClothProductVariantId { get; set; }
    public Guid? ClothBoxPackId { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitRate { get; set; }
}

// --- PURCHASE SUMMARY DTO ---

public class ClothPurchaseSummaryDto
{
    public decimal TotalPurchasesValue { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalOutstandingPayable { get; set; }
    public int TotalBillsCount { get; set; }
    public int TotalSuppliersCount { get; set; }
    public int PendingBillsCount { get; set; }
}
