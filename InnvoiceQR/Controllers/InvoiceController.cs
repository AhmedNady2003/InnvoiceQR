using InnvoiceQR.Requests;
using InnvoiceQR.Services;
using Microsoft.AspNetCore.Mvc;

namespace InnvoiceQR.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly PdfService _pdfService;

        public InvoiceController(IInvoiceService invoiceService, PdfService pdfService)
        {
            _invoiceService = invoiceService;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateInvoiceRequest request)
        {
            var invoice = await _invoiceService.CreateAsync(request);
            var pdf = _pdfService.Generate(invoice);

            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
    }
}
