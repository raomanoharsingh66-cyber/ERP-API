using BizFlow.Api.Attributes;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Inventory;
using BizFlow.Domain.Constants;
using BizFlow.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BizFlow.Api.Controllers;

public class CategoriesController : BaseApiController
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CategoriesController(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    [HttpGet]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .Include(c => c.ParentCategory)
            .Include(c => c.Products)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count(p => !p.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        return OkResponse(categories);
    }

    [HttpPost]
    [HasPermission(Permissions.Inventory.Create)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryDto dto, 
        CancellationToken cancellationToken)
    {
        var businessId = _currentUserService.BusinessId;
        if (!_currentUserService.IsSuperAdmin && businessId == null)
        {
            throw new BusinessRuleException("No active business tenant context in current session.");
        }

        var normalizedCode = dto.Code.Trim().ToUpperInvariant();

        var exists = await _context.Categories
            .AnyAsync(c => c.Code == normalizedCode, cancellationToken);

        if (exists)
        {
            throw new ValidationException(new List<string> { $"Category code '{normalizedCode}' already exists." });
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            BusinessId = businessId!.Value,
            Name = dto.Name.Trim(),
            Code = normalizedCode,
            Description = dto.Description?.Trim(),
            ParentCategoryId = dto.ParentCategoryId,
            IsActive = true,
            CreatedOn = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.Email ?? "System"
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            IsActive = category.IsActive,
            ProductCount = 0
        };

        return OkResponse(resultDto, "Category created successfully.");
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Inventory.Update)]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(
        Guid id, 
        [FromBody] UpdateCategoryDto dto, 
        CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.ParentCategoryId = dto.ParentCategoryId;
        category.IsActive = dto.IsActive;
        category.UpdatedOn = _dateTimeProvider.UtcNow;
        category.UpdatedBy = _currentUserService.Email ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        var resultDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Description = category.Description,
            ParentCategoryId = category.ParentCategoryId,
            IsActive = category.IsActive
        };

        return OkResponse(resultDto, "Category updated successfully.");
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Inventory.Delete)]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(Guid id, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        if (category.Products.Any(p => !p.IsDeleted))
        {
            throw new BusinessRuleException("Cannot delete category with associated products.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return OkResponse("Category removed successfully.");
    }
}
