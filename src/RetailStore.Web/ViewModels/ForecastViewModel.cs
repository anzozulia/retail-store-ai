using Microsoft.AspNetCore.Mvc.Rendering;
using RetailStore.Web.Models;

namespace RetailStore.Web.ViewModels;

public class ForecastViewModel
{
    public int WindowWeeks { get; set; }

    public List<DemandForecast> Rows { get; set; } = new List<DemandForecast>();

    public DateOnly TargetWeek { get; set; }

    public DateTime CalculatedAt { get; set; }

    public DateTime? LastSaleDate { get; set; }

    public int SelectedProductId { get; set; }

    public SelectList Products { get; set; } = new SelectList(new List<Product>());

    public List<string> ChartLabels { get; set; } = new List<string>();

    public List<decimal> ForecastValues { get; set; } = new List<decimal>();

    public List<int?> ActualValues { get; set; } = new List<int?>();

    public int MaeWeeks { get; set; }

    public decimal? Mae { get; set; }

    public decimal? MaePercent { get; set; }
}
