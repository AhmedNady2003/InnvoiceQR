using InnvoiceQR.Services;
using InnvoiceQR.Services.Settings;

namespace InnvoiceQR
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost5500",
                    policy =>
                    {
                        policy.WithOrigins("http://127.0.0.1:5500", "https://aien-elnada-invoice-app.vercel.app")
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
            });

            builder.Services.AddScoped<IZatcaQrService, ZatcaQrService>();
            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddScoped<PdfService>();

            builder.Services.Configure<CompanySettings>(
                builder.Configuration.GetSection("CompanySettings"));

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI(ui =>
                ui.SwaggerEndpoint("/swagger/v1/swagger.json", "Toshka Barber API v1"));

            // ✅ Use CORS هنا قبل Authorization
            app.UseCors("AllowLocalhost5500");

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}