using RetailStore.Web.Models;

namespace RetailStore.Web.ViewModels;

public class SalesIndexViewModel
{
    public List<Sale> Sales { get; set; } = new();

    public string? Search { get; set; }

    public string? From { get; set; }

    public string? To { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }
}
