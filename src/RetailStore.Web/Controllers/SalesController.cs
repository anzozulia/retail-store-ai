using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RetailStore.Web.Data;
using RetailStore.Web.Models;
using RetailStore.Web.ViewModels;

namespace RetailStore.Web.Controllers;

public class SalesController : Controller
{
    private const int PageSize = 25;
    private const int FormLines = 5;

    private readonly RetailStoreContext _context;

    public SalesController(RetailStoreContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, DateTime? from, DateTime? to, int page = 1)
    {
        IQueryable<Sale> query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim().TrimStart('#');

            if (int.TryParse(search, out int saleId))
            {
                query = query.Where(s => s.SaleId == saleId);
            }
            else
            {
                query = query.Where(s => s.Customer != null && s.Customer.Name.Contains(search));
            }
        }

        if (from.HasValue)
        {
            var fromDate = from.Value.Date;
            query = query.Where(s => s.SaleDate >= fromDate);
        }

        if (to.HasValue)
        {
            var toDate = to.Value.Date;
            query = query.Where(s => s.SaleDate.Date <= toDate);
        }

        int totalCount = await query.CountAsync();
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var model = new SalesIndexViewModel
        {
            Search = search,
            From = from?.ToString("yyyy-MM-dd"),
            To = to?.ToString("yyyy-MM-dd"),
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        model.Sales = await query
            .OrderByDescending(s => s.SaleDate)
            .ThenByDescending(s => s.SaleId)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var sale = await FindSaleWithLinesAsync(id);

        if (sale == null)
        {
            return NotFound();
        }

        return View(sale);
    }

    public async Task<IActionResult> Create()
    {
        var now = DateTime.Now;
        var model = new SaleFormViewModel
        {
            SaleDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0)
        };

        return await SaleFormViewAsync(model, FormLines);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleFormViewModel model)
    {
        var items = await ValidateSaleAsync(model);

        if (ModelState.IsValid)
        {
            var sale = new Sale
            {
                SaleDate = model.SaleDate,
                CustomerId = model.CustomerId,
                SaleItems = items
            };

            _context.Sales.Add(sale);

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Sale #{sale.SaleId} saved.";
                return RedirectToAction(nameof(Details), new { id = sale.SaleId });
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 50001)
            {
                ModelState.AddModelError(string.Empty, "Not enough stock for one or more products.");
            }
        }

        return await SaleFormViewAsync(model, FormLines);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.SaleId == id);

        if (sale == null)
        {
            return NotFound();
        }

        var model = new SaleFormViewModel
        {
            SaleId = sale.SaleId,
            SaleDate = sale.SaleDate,
            CustomerId = sale.CustomerId
        };

        foreach (var item in sale.SaleItems.OrderBy(i => i.SaleItemId))
        {
            model.Lines.Add(new SaleLineInput { ProductId = item.ProductId, Quantity = item.Quantity });
        }

        return await SaleFormViewAsync(model, Math.Max(FormLines, model.Lines.Count + 1));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SaleFormViewModel model)
    {
        if (id != model.SaleId)
        {
            return NotFound();
        }

        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.SaleId == id);

        if (sale == null)
        {
            return NotFound();
        }

        var items = await ValidateSaleAsync(model);

        if (ModelState.IsValid)
        {
            sale.SaleDate = model.SaleDate;
            sale.CustomerId = model.CustomerId;

            foreach (var existing in sale.SaleItems.ToList())
            {
                var posted = items.FirstOrDefault(i => i.ProductId == existing.ProductId);

                if (posted == null)
                {
                    _context.SaleItems.Remove(existing);
                }
                else
                {
                    existing.Quantity = posted.Quantity;
                }
            }

            foreach (var item in items)
            {
                if (!sale.SaleItems.Any(i => i.ProductId == item.ProductId))
                {
                    sale.SaleItems.Add(item);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Sale #{sale.SaleId} updated.";
                return RedirectToAction(nameof(Details), new { id = sale.SaleId });
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 50001)
            {
                ModelState.AddModelError(string.Empty, "Not enough stock for one or more products.");
            }
        }

        return await SaleFormViewAsync(model, FormLines);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var sale = await FindSaleWithLinesAsync(id);

        if (sale == null)
        {
            return NotFound();
        }

        return View(sale);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .FirstOrDefaultAsync(s => s.SaleId == id);

        if (sale == null)
        {
            return NotFound();
        }

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Sale #{id} deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Sale?> FindSaleWithLinesAsync(int id)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.SaleItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.SaleId == id);
    }

    private async Task<IActionResult> SaleFormViewAsync(SaleFormViewModel model, int lineCount)
    {
        while (model.Lines.Count < lineCount)
        {
            model.Lines.Add(new SaleLineInput());
        }

        var customers = await _context.Customers.OrderBy(c => c.Name).ToListAsync();
        ViewData["Customers"] = new SelectList(customers, "CustomerId", "Name");

        var products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
        ViewData["Products"] = products
            .Select(p => new SelectListItem
            {
                Value = p.ProductId.ToString(),
                Text = $"{p.Name} (in stock: {p.StockQuantity}, ₴{p.UnitPrice:N2})"
            })
            .ToList();

        return View(model);
    }

    private async Task<List<SaleItem>> ValidateSaleAsync(SaleFormViewModel model)
    {
        var items = new List<SaleItem>();

        if (model.SaleDate > DateTime.Now.AddMinutes(1))
        {
            ModelState.AddModelError(nameof(SaleFormViewModel.SaleDate), "Sale date cannot be in the future.");
        }

        if (model.CustomerId.HasValue && !await _context.Customers.AnyAsync(c => c.CustomerId == model.CustomerId))
        {
            ModelState.AddModelError(nameof(SaleFormViewModel.CustomerId), "Customer not found.");
        }

        if (model.Lines.All(l => l.ProductId == null && l.Quantity == null))
        {
            ModelState.AddModelError(string.Empty, "Add at least one product to the receipt.");
            return items;
        }

        var prices = await _context.Products.AsNoTracking().ToDictionaryAsync(p => p.ProductId, p => p.UnitPrice);

        for (int i = 0; i < model.Lines.Count; i++)
        {
            var line = model.Lines[i];

            if (line.ProductId == null && line.Quantity == null)
            {
                continue;
            }

            if (line.ProductId == null)
            {
                ModelState.AddModelError($"Lines[{i}].ProductId", "Select a product.");
            }
            else if (!prices.ContainsKey(line.ProductId.Value))
            {
                ModelState.AddModelError($"Lines[{i}].ProductId", "Product not found.");
            }
            else if (items.Any(item => item.ProductId == line.ProductId))
            {
                ModelState.AddModelError($"Lines[{i}].ProductId", "This product is already in the receipt.");
            }
            else if (line.Quantity == null)
            {
                ModelState.AddModelError($"Lines[{i}].Quantity", "Enter a quantity.");
            }
            else if (line.Quantity < 1 || line.Quantity > 1000)
            {
                ModelState.AddModelError($"Lines[{i}].Quantity", "Quantity must be between 1 and 1000.");
            }
            else
            {
                items.Add(new SaleItem
                {
                    ProductId = line.ProductId.Value,
                    Quantity = line.Quantity.Value,
                    UnitPrice = prices[line.ProductId.Value]
                });
            }
        }

        return items;
    }
}
