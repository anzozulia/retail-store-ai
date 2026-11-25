namespace RetailStore.Web.ViewModels;

public class SaleFormViewModel
{
    public int SaleId { get; set; }

    public DateTime SaleDate { get; set; }

    public int? CustomerId { get; set; }

    public List<SaleLineInput> Lines { get; set; } = new List<SaleLineInput>();
}
