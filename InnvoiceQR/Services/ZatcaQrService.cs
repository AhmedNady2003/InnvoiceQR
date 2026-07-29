using InnvoiceQR.Entities;
using System.Text;

namespace InnvoiceQR.Services
{
    public interface IZatcaQrService
    {
        string Generate(Invoice invoice);
    }

    public class ZatcaQrService : IZatcaQrService
    {
        public string Generate(Invoice invoice)
        {
            var bytes = new List<byte>();

            void Add(byte tag, string value)
            {
                var valueBytes = Encoding.UTF8.GetBytes(value);
                bytes.Add(tag);
                bytes.Add((byte)valueBytes.Length);
                bytes.AddRange(valueBytes);
            }

            Add(1, invoice.SellerName);
            Add(2, invoice.VatNumber);
            Add(3, invoice.IssueDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            Add(4, invoice.TotalAmount.ToString("0.00"));
            Add(5, invoice.VatAmount.ToString("0.00"));

            return Convert.ToBase64String(bytes.ToArray());
        }
    }
}
