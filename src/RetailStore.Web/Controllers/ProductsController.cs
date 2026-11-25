using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RetailStore.Web.Data;
using RetailStore.Web.Models;

namespace RetailStore.Web.Controllers;

public class ProductsController : Controller
{
    private readonly RetailStoreContext _context;

    public ProductsController(RetailStoreContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        IQueryable<Product> query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(p => p.Name.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync();

        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        ViewData["Categories"] = new SelectList(categories, "CategoryId", "Name", categoryId);
        ViewData["Search"] = search;

        return View(products);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        ViewData["RecentSales"] = await _context.SaleItems
            .Include(i => i.Sale)
            .Where(i => i.ProductId == id)
            .OrderByDescending(i => i.Sale.SaleDate)
            .ThenByDescending(i => i.SaleId)
            .Take(10)
            .ToListAsync();

        return View(product);
    }

    public async Task<IActionResult> Create()
    {
        await FillSelectListsAsync();
        return View(new Product());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,CategoryId,SupplierId,UnitPrice,StockQuantity,MinStock")] Product product)
    {
        await ValidateProductAsync(product);

        if (product.StockQuantity < 0)
        {
            ModelState.AddModelError(nameof(Product.StockQuantity), "Stock cannot be negative.");
        }

        if (ModelState.IsValid)
        {
            _context.Products.Add(product);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Product \"{product.Name}\" added.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                ModelState.AddModelError(nameof(Product.Name), "A product with this name already exists.");
            }
        }

        await FillSelectListsAsync();
        return View(product);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        await FillSelectListsAsync();
        return View(product);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ProductId,Name,CategoryId,SupplierId,UnitPrice,MinStock")] Product product)
    {
        if (id != product.ProductId)
        {
            return NotFound();
        }

        var entity = await _context.Products.FindAsync(id);

        if (entity == null)
        {
            return NotFound();
        }

        await ValidateProductAsync(product);

        if (ModelState.IsValid)
        {
            entity.Name = product.Name;
            entity.CategoryId = product.CategoryId;
            entity.SupplierId = product.SupplierId;
            entity.UnitPrice = product.UnitPrice;
            entity.MinStock = product.MinStock;

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Product \"{entity.Name}\" updated.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                ModelState.AddModelError(nameof(Product.Name), "A product with this name already exists.");
            }
        }

        product.StockQuantity = entity.StockQuantity;
        await FillSelectListsAsync();
        return View(product);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound();
        }

        ViewData["HasSales"] = await _context.SaleItems.AnyAsync(i => i.ProductId == id);
        return View(product);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        if (await _context.SaleItems.AnyAsync(i => i.ProductId == id))
        {
            TempData["Error"] = "This product has sales and cannot be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.Products.Remove(product);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 547)
        {
            TempData["Error"] = "This product has sales and cannot be deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = $"Product \"{product.Name}\" deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillSelectListsAsync()
    {
        var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
        var suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();

        ViewData["Categories"] = new SelectList(categories, "CategoryId", "Name");
        ViewData["Suppliers"] = new SelectList(suppliers, "SupplierId", "Name");
    }

    private async Task ValidateProductAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
        {
            ModelState.AddModelError(nameof(Product.Name), "Name is required.");
        }
        else
        {
            product.Name = product.Name.Trim();

            if (product.Name.Length > 100)
            {
                ModelState.AddModelError(nameof(Product.Name), "Name must be at most 100 characters.");
            }
        }

        if (!await _context.Categories.AnyAsync(c => c.CategoryId == product.CategoryId))
        {
            ModelState.AddModelError(nameof(Product.CategoryId), "Select a category.");
        }

        if (!await _context.Suppliers.AnyAsync(s => s.SupplierId == product.SupplierId))
        {
            ModelState.AddModelError(nameof(Product.SupplierId), "Select a supplier.");
        }

        if (product.UnitPrice <= 0 || product.UnitPrice >= 100000000)
        {
            ModelState.AddModelError(nameof(Product.UnitPrice), "Price must be greater than zero and less than 100,000,000.");
        }
        else if (product.UnitPrice != Math.Round(product.UnitPrice, 2))
        {
            ModelState.AddModelError(nameof(Product.UnitPrice), "Price can have at most 2 decimal places.");
        }

        if (product.MinStock < 0)
        {
            ModelState.AddModelError(nameof(Product.MinStock), "Min stock cannot be negative.");
        }
    }
}
