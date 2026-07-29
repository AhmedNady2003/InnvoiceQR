using InnvoiceQR.Entities;
using InnvoiceQR.Requests;
using InnvoiceQR.Services.Settings;
using Microsoft.Extensions.Options;

namespace InnvoiceQR.Services
{
    public interface IInvoiceService
    {
        Task<Invoice> CreateAsync(CreateInvoiceRequest request);
    }

    

public class InvoiceService : IInvoiceService
    {
        private readonly IZatcaQrService _qrService;
        private readonly CompanySettings _company;

        public InvoiceService(
            IZatcaQrService qrService,
            IOptions<CompanySettings> companyOptions)
        {
            _qrService = qrService;
            _company = companyOptions.Value;
        }

        public async Task<Invoice> CreateAsync(CreateInvoiceRequest request)
        {
            var VatRate = 0.15m; // 15% VAT
            var unitPrice = request.UnitPrice / (1+ VatRate);
            var subTotal = unitPrice * request.Quantity;

            var total = request.UnitPrice * request.Quantity;
            var vatAmount = subTotal * VatRate ;

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = GenerateInvoiceNumber(),

                SellerName = _company.SellerName,
                VatNumber = _company.VatNumber,
                SellerAddress = _company.SellerAddress,
                BuyerName = request.BuyerName,
                BuyerVatNumber = request.BuyerVatNumber,

                ProductDescription = request.ProductDescription,
                UnitPrice = (decimal)unitPrice,
                Quantity = (decimal)request.Quantity,

                SubTotal = (decimal)subTotal,
                VatAmount = (decimal)vatAmount,
                TotalAmount = (decimal)total,

                Uuid = Guid.NewGuid().ToString(),
                IssueDate = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            invoice.QrCodeBase64 = _qrService.Generate(invoice);

            return invoice;
        }

        private string GenerateInvoiceNumber()
        {
            return $"INV-{DateTime.Now:yyyy}-{Random.Shared.Next(100000, 999999)}";
        }
    }
}
