using System;
using System.Collections.Generic;

namespace RetailStore.Web.Models;

public partial class VwWeeklySale
{
    public int ProductId { get; set; }

    public DateOnly? WeekStart { get; set; }

    public int? Quantity { get; set; }

    public decimal? Revenue { get; set; }
}
