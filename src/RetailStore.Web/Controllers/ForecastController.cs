using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RetailStore.Web.Data;
using RetailStore.Web.ViewModels;

namespace RetailStore.Web.Controllers;

public class ForecastController : Controller
{
    private const int WindowWeeks = 4;

    private readonly RetailStoreContext _context;

    public ForecastController(RetailStoreContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? productId)
    {
        var model = new ForecastViewModel { WindowWeeks = WindowWeeks };

        model.Rows = await _context.DemandForecasts
            .Include(f => f.Product).ThenInclude(p => p.Category)
            .Include(f => f.Product).ThenInclude(p => p.Supplier)
            .Where(f => f.RecommendedOrderQty != null)
            .OrderByDescending(f => f.RecommendedOrderQty)
            .ThenBy(f => f.Product.Name)
            .ToListAsync();

        if (model.Rows.Count == 0)
        {
            return View(model);
        }

        model.TargetWeek = model.Rows.Max(f => f.WeekStart);
        model.CalculatedAt = model.Rows.Max(f => f.CalculatedAt);
        model.LastSaleDate = await _context.Sales.MaxAsync(s => (DateTime?)s.SaleDate);

        model.SelectedProductId = model.Rows[0].ProductId;
        if (productId != null && model.Rows.Any(f => f.ProductId == productId))
        {
            model.SelectedProductId = productId.Value;
        }

        model.Products = new SelectList(
            model.Rows.Select(f => f.Product).OrderBy(p => p.Name),
            "ProductId",
            "Name",
            model.SelectedProductId);

        var history = await _context.DemandForecasts
            .Where(f => f.ProductId == model.SelectedProductId)
            .OrderBy(f => f.WeekStart)
            .ToListAsync();

        var firstWeek = history[0].WeekStart;

        var actualWeeks = await _context.VwWeeklySales
            .Where(w => w.ProductId == model.SelectedProductId && w.WeekStart >= firstWeek && w.WeekStart <= model.TargetWeek)
            .ToListAsync();

        decimal errorSum = 0;
        int actualSum = 0;

        foreach (var forecast in history)
        {
            var week = actualWeeks.FirstOrDefault(w => w.WeekStart == forecast.WeekStart);

            model.ChartLabels.Add(forecast.WeekStart.ToString("MMM d"));
            model.ForecastValues.Add(forecast.ForecastQty);

            if (forecast.WeekStart == model.TargetWeek)
            {
                model.ActualValues.Add(week?.Quantity);
            }
            else
            {
                int actual = week?.Quantity ?? 0;
                model.ActualValues.Add(actual);
                errorSum += Math.Abs(forecast.ForecastQty - actual);
                actualSum += actual;
                model.MaeWeeks++;
            }
        }

        if (model.MaeWeeks > 0)
        {
            model.Mae = errorSum / model.MaeWeeks;
        }

        if (actualSum > 0)
        {
            model.MaePercent = errorSum / actualSum * 100;
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate()
    {
        try
        {
            await _context.Database.ExecuteSqlAsync($"EXEC dbo.usp_CalculateDemandForecast @Weeks = {WindowWeeks}");
            TempData["Success"] = "Forecast recalculated.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Restock(int productId, int quantity)
    {
        try
        {
            await _context.Database.ExecuteSqlAsync($"EXEC dbo.usp_RestockProduct @ProductId = {productId}, @Quantity = {quantity}");
        }
        catch (SqlException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { productId });
        }

        var productName = await _context.Products
            .Where(p => p.ProductId == productId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync();

        try
        {
            await _context.Database.ExecuteSqlAsync($"EXEC dbo.usp_CalculateDemandForecast @Weeks = {WindowWeeks}");
            TempData["Success"] = $"Received {quantity} units of {productName}. Forecast recalculated.";
        }
        catch (SqlException ex)
        {
            TempData["Error"] = $"Received {quantity} units of {productName}, but the forecast was not recalculated: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { productId });
    }
}
