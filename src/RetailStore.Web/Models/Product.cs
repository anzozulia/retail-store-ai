using System;
using System.Collections.Generic;

namespace RetailStore.Web.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public int CategoryId { get; set; }

    public int SupplierId { get; set; }

    public decimal UnitPrice { get; set; }

    public int StockQuantity { get; set; }

    public int MinStock { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<DemandForecast> DemandForecasts { get; set; } = new List<DemandForecast>();

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual Supplier Supplier { get; set; } = null!;
}
