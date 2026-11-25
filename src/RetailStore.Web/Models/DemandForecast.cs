using System;
using System.Collections.Generic;

namespace RetailStore.Web.Models;

public partial class DemandForecast
{
    public int ForecastId { get; set; }

    public int ProductId { get; set; }

    public DateOnly WeekStart { get; set; }

    public decimal ForecastQty { get; set; }

    public int? RecommendedOrderQty { get; set; }

    public DateTime CalculatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
