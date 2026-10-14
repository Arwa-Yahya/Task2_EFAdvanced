using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task2_EF.Data;
using Task2_EF.DTOs;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /api/products?pageNumber=1&pageSize=10
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        if (pageNumber < 1 || pageSize < 1)
        {
            return BadRequest("Page number and page size must be greater than zero.");
        }

        pageSize = Math.Min(pageSize, 100);

        var query = _context.Products.AsNoTracking();

        var totalCount = await query.CountAsync();

        var products = await query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                RowVersion = p.RowVersion
            })
            .ToListAsync();

        return Ok(new
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = products
        });
    }

    // PUT: /api/products/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest("Product name is required.");
        }

        if (dto.Price < 0)
        {
            return BadRequest("Price cannot be negative.");
        }

        if (dto.RowVersion.Length == 0)
        {
            return BadRequest("RowVersion is required.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId);

        if (!categoryExists)
        {
            return BadRequest("The specified category does not exist.");
        }

        _context.Entry(product)
            .Property(p => p.RowVersion)
            .OriginalValue = dto.RowVersion;

        product.Name = dto.Name;
        product.Price = dto.Price;
        product.CategoryId = dto.CategoryId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                Message = "The product was modified by another user. Reload the latest data and try again."
            });
        }

        return NoContent();
    }

    // DELETE: /api/products/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        product.IsDeleted = true;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}