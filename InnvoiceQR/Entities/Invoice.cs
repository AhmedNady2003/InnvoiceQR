namespace InnvoiceQR.Entities
{
    public class Invoice
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string SellerName { get; set; }
        public string VatNumber { get; set; }
        public string SellerAddress { get; set; }
        public string BuyerName { get; set; }
        public string BuyerVatNumber { get; set; }
        public string ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Quantity { get; set; }

        public decimal SubTotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string QrCodeBase64 { get; set; }
        public string Uuid { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
