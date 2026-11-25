using RetailStore.Web.Models;

namespace RetailStore.Web.ViewModels;

public class DashboardViewModel
{
    public decimal Revenue { get; set; }

    public int Receipts { get; set; }

    public decimal AverageReceipt { get; set; }

    public int LowStockCount { get; set; }

    public DateTime? FirstSaleDate { get; set; }

    public DateTime? LastSaleDate { get; set; }

    public List<string> WeekLabels { get; set; } = new List<string>();

    public List<decimal> WeekRevenue { get; set; } = new List<decimal>();

    public List<string> TopProductNames { get; set; } = new List<string>();

    public List<decimal> TopProductRevenue { get; set; } = new List<decimal>();

    public List<string> CategoryNames { get; set; } = new List<string>();

    public List<decimal> CategoryRevenue { get; set; } = new List<decimal>();

    public List<Product> LowStockProducts { get; set; } = new List<Product>();
}
