using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailStore.Web.Data;
using RetailStore.Web.Models;
using RetailStore.Web.ViewModels;

namespace RetailStore.Web.Controllers;

public class HomeController : Controller
{
    private readonly RetailStoreContext _context;

    public HomeController(RetailStoreContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();

        model.Revenue = await _context.SaleItems.SumAsync(i => i.Quantity * i.UnitPrice);
        model.Receipts = await _context.Sales.CountAsync(s => s.SaleItems.Any());
        model.AverageReceipt = model.Receipts > 0 ? model.Revenue / model.Receipts : 0;
        model.FirstSaleDate = await _context.Sales.MinAsync(s => (DateTime?)s.SaleDate);
        model.LastSaleDate = await _context.Sales.MaxAsync(s => (DateTime?)s.SaleDate);

        var weeks = await _context.VwWeeklySales
            .GroupBy(w => w.WeekStart)
            .Select(g => new { WeekStart = g.Key, Revenue = g.Sum(w => w.Revenue) })
            .OrderBy(x => x.WeekStart)
            .ToListAsync();

        foreach (var week in weeks)
        {
            model.WeekLabels.Add(week.WeekStart?.ToString("MMM d") ?? "");
            model.WeekRevenue.Add(week.Revenue ?? 0);
        }

        var topProducts = await _context.SaleItems
            .GroupBy(i => i.Product.Name)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(i => i.Quantity * i.UnitPrice) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync();

        foreach (var product in topProducts)
        {
            model.TopProductNames.Add(product.Name);
            model.TopProductRevenue.Add(product.Revenue);
        }

        var categories = await _context.SaleItems
            .GroupBy(i => i.Product.Category.Name)
            .Select(g => new { Name = g.Key, Revenue = g.Sum(i => i.Quantity * i.UnitPrice) })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        foreach (var category in categories)
        {
            model.CategoryNames.Add(category.Name);
            model.CategoryRevenue.Add(category.Revenue);
        }

        model.LowStockProducts = await _context.Products
            .Include(p => p.Category)
            .Where(p => p.StockQuantity <= p.MinStock)
            .OrderBy(p => p.StockQuantity - p.MinStock)
            .ToListAsync();
        model.LowStockCount = model.LowStockProducts.Count;

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
