using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.ClothHub;
using BizFlow.Domain.Entities.ClothHub;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Infrastructure.Services.ClothHub;

public class ClothPurchaseService : IClothPurchaseService
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClothPurchaseService(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    private Guid GetEffectiveBusinessId()
    {
        return _currentUserService.BusinessId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    private async Task EnsureDefaultSuppliersAsync(CancellationToken cancellationToken)
    {
        if (await _context.ClothSuppliers.AnyAsync(cancellationToken))
            return;

        var businessId = GetEffectiveBusinessId();
        var defaultSuppliers = new List<ClothSupplier>
        {
            new()
            {
                BusinessId = businessId,
                SupplierName = "Raymond Fabrics & Apparel Ltd",
                ContactPerson = "Rajesh Sharma",
                Phone = "+91 98200 11223",
                Email = "orders@raymondmills.com",
                Gstin = "27AAACR1234F1Z5",
                Address = "Plot 42, Textile SEZ, Thane",
                City = "Mumbai",
                State = "Maharashtra",
                Pincode = "400601",
                PaymentTerms = "Net 30 Days",
                CreditLimit = 1500000,
                CurrentPayableBalance = 145000
            },
            new()
            {
                BusinessId = businessId,
                SupplierName = "Surat Textile Saree & Kurti Hub",
                ContactPerson = "Ketan Patel",
                Phone = "+91 98980 44556",
                Email = "sales@surattextilehub.in",
                Gstin = "24AABCS5678K1Z9",
                Address = "Ring Road Wholesale Market",
                City = "Surat",
                State = "Gujarat",
                Pincode = "395002",
                PaymentTerms = "Net 15 Days",
                CreditLimit = 800000,
                CurrentPayableBalance = 82500
            },
            new()
            {
                BusinessId = businessId,
                SupplierName = "Ludhiana Knitwear & Woolens Mill",
                ContactPerson = "Harpreet Singh",
                Phone = "+91 98140 77889",
                Email = "supply@ludhianaknitwear.com",
                Gstin = "03AAACL9012M1Z2",
                Address = "Industrial Area A",
                City = "Ludhiana",
                State = "Punjab",
                Pincode = "141003",
                PaymentTerms = "Net 45 Days",
                CreditLimit = 1200000,
                CurrentPayableBalance = 0
            },
            new()
            {
                BusinessId = businessId,
                SupplierName = "Tirupur Cotton Hosiery Exporters",
                ContactPerson = "M. Senthil Kumar",
                Phone = "+91 94430 33445",
                Email = "exports@tirupurcotton.com",
                Gstin = "33AAACT3456P1Z8",
                Address = "Avinashi Road Garment Park",
                City = "Tirupur",
                State = "Tamil Nadu",
                Pincode = "641603",
                PaymentTerms = "Net 30 Days",
                CreditLimit = 2000000,
                CurrentPayableBalance = 65000
            }
        };

        _context.ClothSuppliers.AddRange(defaultSuppliers);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // --- 1. SUPPLIERS ---
    public async Task<List<ClothSupplierDto>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultSuppliersAsync(cancellationToken);

        return await _context.ClothSuppliers
            .AsNoTracking()
            .OrderBy(s => s.SupplierName)
            .Select(s => new ClothSupplierDto
            {
                Id = s.Id,
                SupplierName = s.SupplierName,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                Gstin = s.Gstin,
                Address = s.Address,
                City = s.City,
                State = s.State,
                Pincode = s.Pincode,
                PaymentTerms = s.PaymentTerms,
                CreditLimit = s.CreditLimit,
                CurrentPayableBalance = s.CurrentPayableBalance,
                IsActive = s.IsActive,
                BillsCount = _context.ClothPurchaseBills.Count(b => b.ClothSupplierId == s.Id)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClothSupplierDto> CreateSupplierAsync(CreateClothSupplierDto dto, CancellationToken cancellationToken = default)
    {
        var supplier = new ClothSupplier
        {
            BusinessId = GetEffectiveBusinessId(),
            SupplierName = dto.SupplierName.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Gstin = dto.Gstin?.Trim().ToUpper(),
            Address = dto.Address?.Trim(),
            City = dto.City?.Trim(),
            State = dto.State?.Trim(),
            Pincode = dto.Pincode?.Trim(),
            PaymentTerms = dto.PaymentTerms,
            CreditLimit = dto.CreditLimit > 0 ? dto.CreditLimit : 500000,
            CurrentPayableBalance = 0,
            IsActive = true
        };

        _context.ClothSuppliers.Add(supplier);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClothSupplierDto
        {
            Id = supplier.Id,
            SupplierName = supplier.SupplierName,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Gstin = supplier.Gstin,
            Address = supplier.Address,
            City = supplier.City,
            State = supplier.State,
            Pincode = supplier.Pincode,
            PaymentTerms = supplier.PaymentTerms,
            CreditLimit = supplier.CreditLimit,
            CurrentPayableBalance = supplier.CurrentPayableBalance,
            IsActive = supplier.IsActive,
            BillsCount = 0
        };
    }

    // --- 2. PURCHASE BILLS & INWARD ---
    public async Task<List<ClothPurchaseBillDto>> GetPurchaseBillsAsync(CancellationToken cancellationToken = default)
    {
        var bills = await _context.ClothPurchaseBills
            .AsNoTracking()
            .Include(b => b.Supplier)
            .Include(b => b.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Size)
            .Include(b => b.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Colour)
            .Include(b => b.Items)
                .ThenInclude(i => i.BoxPack)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(cancellationToken);

        return bills.Select(b => MapToBillDto(b)).ToList();
    }

    public async Task<ClothPurchaseBillDto?> GetPurchaseBillByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bill = await _context.ClothPurchaseBills
            .AsNoTracking()
            .Include(b => b.Supplier)
            .Include(b => b.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Size)
            .Include(b => b.Items)
                .ThenInclude(i => i.Variant)
                    .ThenInclude(v => v!.Colour)
            .Include(b => b.Items)
                .ThenInclude(i => i.BoxPack)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        return bill != null ? MapToBillDto(bill) : null;
    }

    public async Task<ClothPurchaseBillDto> CreatePurchaseBillAsync(CreateClothPurchaseBillDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("At least 1 item is required to generate a purchase bill.");

        var businessId = GetEffectiveBusinessId();
        var now = DateTime.UtcNow;

        var supplier = await _context.ClothSuppliers.FirstOrDefaultAsync(s => s.Id == dto.ClothSupplierId, cancellationToken);
        if (supplier == null)
            throw new KeyNotFoundException("Apparel supplier not found.");

        var count = await _context.ClothPurchaseBills.CountAsync(cancellationToken);
        var billNumber = $"PB-{now:yyyyMMdd}-{(count + 1):D4}";

        decimal subTotal = 0;
        decimal taxAmount = 0;

        var bill = new ClothPurchaseBill
        {
            BusinessId = businessId,
            ClothSupplierId = dto.ClothSupplierId,
            BillNumber = billNumber,
            SupplierInvoiceNumber = !string.IsNullOrWhiteSpace(dto.SupplierInvoiceNumber) ? dto.SupplierInvoiceNumber.Trim() : billNumber,
            BillDate = dto.BillDate,
            DueDate = dto.DueDate ?? dto.BillDate.AddDays(30),
            DiscountAmount = dto.DiscountAmount,
            InwardStatus = dto.InwardStatus,
            Notes = dto.Notes
        };

        foreach (var itemDto in dto.Items)
        {
            var lineSub = itemDto.Quantity * itemDto.UnitRate;
            var lineTax = lineSub * (itemDto.GstRate / 100m);
            var lineTotal = lineSub + lineTax - itemDto.DiscountAmount;

            subTotal += lineSub;
            taxAmount += lineTax;

            var billItem = new ClothPurchaseBillItem
            {
                ItemType = itemDto.ItemType,
                ClothProductVariantId = itemDto.ClothProductVariantId,
                ClothBoxPackId = itemDto.ClothBoxPackId,
                ItemDescription = itemDto.ItemDescription,
                Quantity = itemDto.Quantity,
                UnitRate = itemDto.UnitRate,
                GstRate = itemDto.GstRate,
                GstAmount = lineTax,
                DiscountAmount = itemDto.DiscountAmount,
                LineTotal = lineTotal
            };

            bill.Items.Add(billItem);

            // If InwardStatus is "Received", immediately post inward stock to variants or box packs
            if (dto.InwardStatus == "Received")
            {
                if (itemDto.ItemType == "VariantLoose" && itemDto.ClothProductVariantId.HasValue)
                {
                    var variant = await _context.ClothProductVariants.FirstOrDefaultAsync(v => v.Id == itemDto.ClothProductVariantId.Value, cancellationToken);
                    if (variant != null)
                    {
                        variant.CurrentStock += itemDto.Quantity;

                        _context.ClothStockLedgers.Add(new ClothStockLedger
                        {
                            BusinessId = businessId,
                            ClothProductVariantId = variant.Id,
                            TransactionDate = now,
                            TransactionType = "PurchaseInward",
                            ReferenceNumber = billNumber,
                            QuantityIn = itemDto.Quantity,
                            QuantityOut = 0,
                            RunningBalance = variant.CurrentStock,
                            UnitCost = itemDto.UnitRate,
                            Notes = $"Inward Purchase from {supplier.SupplierName} (Inv: {bill.SupplierInvoiceNumber})"
                        });
                    }
                }
                else if (itemDto.ItemType == "BoxPack" && itemDto.ClothBoxPackId.HasValue)
                {
                    var boxPack = await _context.ClothBoxPacks.FirstOrDefaultAsync(b => b.Id == itemDto.ClothBoxPackId.Value, cancellationToken);
                    if (boxPack != null)
                    {
                        boxPack.CurrentBoxStock += itemDto.Quantity;
                    }
                }
            }
        }

        var totalAmount = subTotal + taxAmount - dto.DiscountAmount;
        bill.SubTotal = subTotal;
        bill.TaxAmount = taxAmount;
        bill.TotalAmount = totalAmount;
        bill.PaidAmount = dto.PaidAmount;
        bill.BalanceAmount = Math.Max(0, totalAmount - dto.PaidAmount);

        if (bill.PaidAmount >= totalAmount)
            bill.PaymentStatus = "Paid";
        else if (bill.PaidAmount > 0)
            bill.PaymentStatus = "PartiallyPaid";
        else
            bill.PaymentStatus = "Unpaid";

        // Update supplier outstanding balance with the remaining unpaid balance
        supplier.CurrentPayableBalance += bill.BalanceAmount;

        _context.ClothPurchaseBills.Add(bill);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetPurchaseBillsAsync(cancellationToken)).First(b => b.Id == bill.Id);
    }

    public async Task<bool> RecordSupplierPaymentAsync(Guid billId, RecordSupplierPaymentDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        var bill = await _context.ClothPurchaseBills
            .Include(b => b.Supplier)
            .FirstOrDefaultAsync(b => b.Id == billId, cancellationToken);

        if (bill == null)
            throw new KeyNotFoundException("Purchase bill not found.");

        var payableReduction = Math.Min(dto.Amount, bill.BalanceAmount);

        bill.PaidAmount += dto.Amount;
        bill.BalanceAmount = Math.Max(0, bill.TotalAmount - bill.PaidAmount);

        if (bill.BalanceAmount == 0)
            bill.PaymentStatus = "Paid";
        else
            bill.PaymentStatus = "PartiallyPaid";

        if (bill.Supplier != null)
        {
            bill.Supplier.CurrentPayableBalance = Math.Max(0, bill.Supplier.CurrentPayableBalance - payableReduction);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    // --- 3. PURCHASE RETURNS & DEBIT NOTES ---
    public async Task<List<ClothPurchaseReturnDto>> GetPurchaseReturnsAsync(CancellationToken cancellationToken = default)
    {
        var returns = await _context.ClothPurchaseReturns
            .AsNoTracking()
            .Include(pr => pr.Supplier)
            .Include(pr => pr.OriginalBill)
            .Include(pr => pr.Items)
                .ThenInclude(i => i.Variant)
            .Include(pr => pr.Items)
                .ThenInclude(i => i.BoxPack)
            .OrderByDescending(pr => pr.ReturnDate)
            .ToListAsync(cancellationToken);

        return returns.Select(pr => new ClothPurchaseReturnDto
        {
            Id = pr.Id,
            ClothSupplierId = pr.ClothSupplierId,
            SupplierName = pr.Supplier?.SupplierName ?? "Supplier",
            ClothPurchaseBillId = pr.ClothPurchaseBillId,
            BillNumber = pr.OriginalBill?.BillNumber,
            DebitNoteNumber = pr.DebitNoteNumber,
            ReturnDate = pr.ReturnDate,
            TotalQuantity = pr.TotalQuantity,
            TotalReturnAmount = pr.TotalReturnAmount,
            Reason = pr.Reason,
            Remarks = pr.Remarks,
            Items = pr.Items.Select(i => new ClothPurchaseReturnItemDto
            {
                Id = i.Id,
                ClothProductVariantId = i.ClothProductVariantId,
                VariantSku = i.Variant?.Sku,
                ClothBoxPackId = i.ClothBoxPackId,
                BoxPackCode = i.BoxPack?.PackCode,
                ItemDescription = i.ItemDescription,
                Quantity = i.Quantity,
                UnitRate = i.UnitRate,
                TotalAmount = i.TotalAmount
            }).ToList()
        }).ToList();
    }

    public async Task<ClothPurchaseReturnDto> CreatePurchaseReturnAsync(CreateClothPurchaseReturnDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new InvalidOperationException("At least 1 item must be specified for vendor purchase return.");

        var businessId = GetEffectiveBusinessId();
        var now = DateTime.UtcNow;

        var supplier = await _context.ClothSuppliers.FirstOrDefaultAsync(s => s.Id == dto.ClothSupplierId, cancellationToken);
        if (supplier == null)
            throw new KeyNotFoundException("Supplier not found.");

        var count = await _context.ClothPurchaseReturns.CountAsync(cancellationToken);
        var debitNoteNumber = $"DN-{now:yyyyMMdd}-{(count + 1):D4}";

        var pReturn = new ClothPurchaseReturn
        {
            BusinessId = businessId,
            ClothSupplierId = dto.ClothSupplierId,
            ClothPurchaseBillId = dto.ClothPurchaseBillId,
            DebitNoteNumber = debitNoteNumber,
            ReturnDate = now,
            Reason = dto.Reason,
            Remarks = dto.Remarks
        };

        var totalQty = 0;
        decimal totalAmount = 0;

        foreach (var itemDto in dto.Items)
        {
            var lineTotal = itemDto.Quantity * itemDto.UnitRate;
            totalQty += itemDto.Quantity;
            totalAmount += lineTotal;

            pReturn.Items.Add(new ClothPurchaseReturnItem
            {
                ClothProductVariantId = itemDto.ClothProductVariantId,
                ClothBoxPackId = itemDto.ClothBoxPackId,
                ItemDescription = itemDto.ItemDescription,
                Quantity = itemDto.Quantity,
                UnitRate = itemDto.UnitRate,
                TotalAmount = lineTotal
            });

            // Deduct stock and write to Stock Ledger
            if (itemDto.ClothProductVariantId.HasValue)
            {
                var variant = await _context.ClothProductVariants.FirstOrDefaultAsync(v => v.Id == itemDto.ClothProductVariantId.Value, cancellationToken);
                if (variant != null)
                {
                    variant.CurrentStock = Math.Max(0, variant.CurrentStock - itemDto.Quantity);

                    _context.ClothStockLedgers.Add(new ClothStockLedger
                    {
                        BusinessId = businessId,
                        ClothProductVariantId = variant.Id,
                        TransactionDate = now,
                        TransactionType = "PurchaseReturn",
                        ReferenceNumber = debitNoteNumber,
                        QuantityIn = 0,
                        QuantityOut = itemDto.Quantity,
                        RunningBalance = variant.CurrentStock,
                        UnitCost = itemDto.UnitRate,
                        Notes = $"Debit Note Return to {supplier.SupplierName}: {dto.Reason}. {dto.Remarks}"
                    });
                }
            }
            else if (itemDto.ClothBoxPackId.HasValue)
            {
                var boxPack = await _context.ClothBoxPacks.FirstOrDefaultAsync(b => b.Id == itemDto.ClothBoxPackId.Value, cancellationToken);
                if (boxPack != null)
                {
                    boxPack.CurrentBoxStock = Math.Max(0, boxPack.CurrentBoxStock - itemDto.Quantity);
                }
            }
        }

        pReturn.TotalQuantity = totalQty;
        pReturn.TotalReturnAmount = totalAmount;

        // Deduct from supplier payable balance via Debit Note
        supplier.CurrentPayableBalance = Math.Max(0, supplier.CurrentPayableBalance - totalAmount);

        _context.ClothPurchaseReturns.Add(pReturn);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetPurchaseReturnsAsync(cancellationToken)).First(pr => pr.Id == pReturn.Id);
    }

    // --- 4. PURCHASE SUMMARY ---
    public async Task<ClothPurchaseSummaryDto> GetPurchaseSummaryAsync(CancellationToken cancellationToken = default)
    {
        var bills = await _context.ClothPurchaseBills.AsNoTracking().ToListAsync(cancellationToken);
        var suppliers = await _context.ClothSuppliers.AsNoTracking().ToListAsync(cancellationToken);

        return new ClothPurchaseSummaryDto
        {
            TotalPurchasesValue = bills.Sum(b => b.TotalAmount),
            TotalPaidAmount = bills.Sum(b => b.PaidAmount),
            TotalOutstandingPayable = suppliers.Sum(s => s.CurrentPayableBalance),
            TotalBillsCount = bills.Count,
            TotalSuppliersCount = suppliers.Count,
            PendingBillsCount = bills.Count(b => b.PaymentStatus != "Paid")
        };
    }

    private static ClothPurchaseBillDto MapToBillDto(ClothPurchaseBill b)
    {
        return new ClothPurchaseBillDto
        {
            Id = b.Id,
            ClothSupplierId = b.ClothSupplierId,
            SupplierName = b.Supplier?.SupplierName ?? "Supplier",
            SupplierGstin = b.Supplier?.Gstin,
            BillNumber = b.BillNumber,
            SupplierInvoiceNumber = b.SupplierInvoiceNumber,
            BillDate = b.BillDate,
            DueDate = b.DueDate,
            SubTotal = b.SubTotal,
            TaxAmount = b.TaxAmount,
            DiscountAmount = b.DiscountAmount,
            RoundOff = b.RoundOff,
            TotalAmount = b.TotalAmount,
            PaidAmount = b.PaidAmount,
            BalanceAmount = b.BalanceAmount,
            PaymentStatus = b.PaymentStatus,
            InwardStatus = b.InwardStatus,
            Notes = b.Notes,
            TotalPieces = b.Items.Sum(i => i.Quantity),
            Items = b.Items.Select(i => new ClothPurchaseBillItemDto
            {
                Id = i.Id,
                ItemType = i.ItemType,
                ClothProductVariantId = i.ClothProductVariantId,
                VariantSku = i.Variant?.Sku,
                SizeName = i.Variant?.Size?.Name,
                ColourName = i.Variant?.Colour?.Name,
                ClothBoxPackId = i.ClothBoxPackId,
                BoxPackCode = i.BoxPack?.PackCode,
                ItemDescription = i.ItemDescription,
                Quantity = i.Quantity,
                UnitRate = i.UnitRate,
                GstRate = i.GstRate,
                GstAmount = i.GstAmount,
                DiscountAmount = i.DiscountAmount,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }
}
