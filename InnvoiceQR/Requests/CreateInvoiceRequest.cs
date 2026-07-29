using System.Numerics;

namespace InnvoiceQR.Requests
{
    public class CreateInvoiceRequest
    {
       

        public string? ProductDescription { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; } = default(decimal?);
        public decimal? Quantity { get; set; } = 0;
        public string? BuyerName { get; set; } = string.Empty;
        public string? BuyerVatNumber { get; set; } = string.Empty;
    }
}
