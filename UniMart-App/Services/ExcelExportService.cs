using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using UniMart_App.Models;

namespace UniMart_App.Services
{
    public class ExcelExportService
    {
        public byte[] ExportOrdersToExcel(IEnumerable<Order> orders, decimal taxPercentage, decimal shippingAmount)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Orders");
                // Header
                worksheet.Cell(1, 1).Value = "Order ID";
                worksheet.Cell(1, 2).Value = "Order Date";
                worksheet.Cell(1, 3).Value = "Customer";
                worksheet.Cell(1, 4).Value = "Email";
                worksheet.Cell(1, 5).Value = "Phone";
                worksheet.Cell(1, 6).Value = "Shipping Address";
                worksheet.Cell(1, 7).Value = "Tracking #";
                worksheet.Cell(1, 8).Value = "Status";
                worksheet.Cell(1, 9).Value = "Delivery Date";
                worksheet.Cell(1, 10).Value = "Total";
                worksheet.Cell(1, 11).Value = "Order Items";

                int row = 2;
                foreach (var order in orders)
                {
                    worksheet.Cell(row, 1).Value = order.Id;
                    worksheet.Cell(row, 2).Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
                    worksheet.Cell(row, 3).Value = order.User?.FullName;
                    worksheet.Cell(row, 4).Value = order.User?.Email;
                    worksheet.Cell(row, 5).Value = order.PhoneNumber;
                    worksheet.Cell(row, 6).Value = order.ShippingAddress;
                    worksheet.Cell(row, 7).Value = order.TrackingNumber;
                    worksheet.Cell(row, 8).Value = order.ShippingStatus;
                    worksheet.Cell(row, 9).Value = order.DeliveryDate?.ToString("yyyy-MM-dd");
                    decimal tax = order.TotalAmount * (taxPercentage / 100);
                    decimal grandTotal = order.TotalAmount + tax + shippingAmount;
                    worksheet.Cell(row, 10).Value = grandTotal;
                    worksheet.Cell(row, 11).Value = string.Join("; ", order.OrderItems?.Select(oi => $"{oi.Product?.Name} x{oi.Quantity} @ {oi.Price:N2}") ?? new string[0]);
                    row++;
                }
                worksheet.Columns().AdjustToContents();
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}
