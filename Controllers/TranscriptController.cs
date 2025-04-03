
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rps.Data;
using rps.Models;
using rps.Services;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;

namespace rps.Controllers
{
     [ApiController]
     [Route("api/[controller]")]
    public class TranscriptController : Controller
    {
       
        private readonly ApplicationDbContext _context;
        private readonly TranscriptService _transcriptService;
        public TranscriptController(TranscriptService transcriptService, ApplicationDbContext context)
        {
            _transcriptService = transcriptService;
            _context = context;
        }
        [HttpPost("paystack-form")]
        public async Task<Order> GetPaymentForm([FromBody] TranscriptDTO transcriptDTO)
        {
            
            var submittedApp = await _transcriptService.SubmitTranscriptPaymentDetails(transcriptDTO.email, transcriptDTO.type, transcriptDTO.amount,transcriptDTO.refNo);
            return submittedApp;
        }

        [HttpGet("success")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string reference)
        {
            try
            {
                // Fetch the order using the reference number
                var order = await _context.Orders.Where(o => o.RefNo == reference).FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound("Order not found.");
                }

                // Update the order status to "Paid"
                order.Status = "Successful";
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                // Redirect to a success page or return a success response
                return Redirect($"/payment-success?reference={reference}"); // Redirect to a frontend success page
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error processing payment success");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpPost("start-or-track-transcript")]
        public async Task<IActionResult> StartOrTrack([FromForm] string? refNo)
        {
            try
            {
                // Check if the payment is available and successful
                var order = await _context.Orders
                    .Where(x => x.RefNo == refNo && x.Status == "Successful")
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    // If the order is not found or not successful
                    return NotFound($"Your reference number {refNo} was not found or the payment was not successful.");
                }

                // Check if an application already exists for this transaction
                var application = await _context.TranscriptApplications
                    .Where(x => x.TransactionId == refNo)
                    .FirstOrDefaultAsync();

                if (application != null)
                {
                    // If the application exists, redirect to the progress page with the application ID
                    return RedirectToAction("progress", "home", new { id = application.Id });
                }
                else
                {
                    // If no application exists, redirect to the application page
                    return Redirect($"/transcript/application?orderId={refNo}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you can use a logging framework like Serilog, NLog, etc.)
                // _logger.LogError(ex, "An error occurred while processing the request.");

                // Return a 500 Internal Server Error with a generic error message
                return StatusCode(500, "An error occurred while processing your request. Please try again later.");
            }
        }   
        [HttpPost("application")]
        public async Task<IActionResult> SubmitTranscriptApplication([FromBody] ApplicationDTO application)
        {
            try
            {
                
               var order = await _context.Orders
                    .Where(x => x.RefNo == application.transactionId && x.Status == "Successful")
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    // If the order is not found or not successful
                    return NotFound("Your reference number was not found or the payment was not successful.");
                }

                var submittedApp = await _transcriptService.TranscriptApplication(application.transactionId, application.name, application.matNo, application.email, application.phoneNo, application.program, application.destinationEmail, application.destinationName, application.note);
                return Ok(submittedApp);
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error submitting transcript application");
                 return null;
            }
        }






        [HttpGet("generate")]
        public IActionResult GenerateTranscript()
        {
            byte[] pdfBytes;

            using (MemoryStream ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Add Header Page
                document.Add(new Paragraph("EDO UNIVERSITY IYAMHO")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20)
                    );

                document.Add(new Paragraph("Address: School Address Here")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12));

                document.Add(new Paragraph("\nThis is to certify that the student listed below has successfully completed their coursework.")
                    .SetTextAlignment(TextAlignment.JUSTIFIED)
                    .SetFontSize(12));

                document.Add(new AreaBreak()); // Move to next page for transcript

                // Add Watermark on all pages
                AddWatermark(pdf, "www.edouniversity.edu.ng", "wwwroot/assets/images/logo/logo.png");

                // Add Transcript Data (Example)
                Table table = new Table(new float[] { 2, 4, 2, 2 });
                table.SetWidth(UnitValue.CreatePercentValue(100));

                table.AddHeaderCell("Course Code");
                table.AddHeaderCell("Course Title");
                table.AddHeaderCell("Credits");
                table.AddHeaderCell("Grade");

                // Example Transcript Data
                string[,] courses = {
                    { "CS101", "Introduction to Computer Science", "3", "A" },
                    { "MTH102", "Calculus II", "4", "B+" },
                    { "PHY103", "Physics II", "3", "A-" }
                };

                for (int i = 0; i < courses.GetLength(0); i++)
                {
                    for (int j = 0; j < courses.GetLength(1); j++)
                    {
                        table.AddCell(courses[i, j]);
                    }
                }

                document.Add(new Paragraph("Transcript Details")
                    
                    .SetFontSize(14)
                    .SetMarginTop(10));

                document.Add(table);
                document.Close();

                pdfBytes = ms.ToArray();
            }

            return File(pdfBytes, "application/pdf", "Transcript.pdf");
        }

        private void AddWatermark(PdfDocument pdf, string watermarkText, string logoPath)
        {
            int totalPages = pdf.GetNumberOfPages();
            for (int i = 1; i <= totalPages; i++)
            {
                PdfPage page = pdf.GetPage(i);
                PdfCanvas canvas = new PdfCanvas(page);
                canvas.SaveState();

                PdfExtGState gs1 = new PdfExtGState();
                gs1.SetFillOpacity(0.2f);
                canvas.SetExtGState(gs1);
                
                PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                                // Add Watermark Text
                            canvas.BeginText()
                    .SetFontAndSize(font, 40) // ✅ Use PdfFont object
                    .MoveText(45, 400)
                    .ShowText(watermarkText)
                    .EndText();

                // Add Logo Watermark
                iText.Layout.Element.Image img = new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(logoPath))
                    .SetOpacity(0.2f)
                    .SetFixedPosition(200, 300)
                    .ScaleToFit(200, 200);

                new Document(pdf).Add(img);
                canvas.RestoreState();
            }
        }
    }

    public class TranscriptDTO
    {
        public string email { get; set; }
        public string type { get; set; }
        public double amount { get; set; } 
        public string refNo { get; set; }
    }

    public class ApplicationDTO
    {
        public string transactionId { get; set; }
        public string email {get; set; }
        public string name { get; set; }
        public string matNo { get; set; }
        public string phoneNo { get; set; }
        public string program { get; set; }
        public string destinationName { get; set; }
        public string destinationEmail { get; set; }
        public string note { get; set; }

    }
    }