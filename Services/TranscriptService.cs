
using rps.Models;
using rps.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace rps.Services
{
    public class TranscriptService
    {
         private readonly ApplicationDbContext _context;
         public TranscriptService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order> SubmitTranscriptPaymentDetails(string email, string type, double amount, string refNo)
        {
            try
            {
                var transcript = new Order
                {
                    Email = email,
                    Type = type,
                    Amount = amount,
                    RefNo = refNo,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                await _context.Orders.AddAsync(transcript);
                await _context.SaveChangesAsync();
                return transcript;
            }
            catch(Exception ex){
                 return null ;
            }
        }

        public async Task<TranscriptApplication> TranscriptApplication(string transactionId, string name, string matNo, string email, string phoneNo, string program, string destinationName, 
        string destinationEmail, string note)
        {
            var transcript = new TranscriptApplication
            {
                TransactionId = transactionId,
                Name = name,
                MatNo = matNo,
                Email = email,
                PhoneNo = phoneNo,
                Program = program,
                DestinationName = destinationName,
                DestinationEmail = destinationEmail,
                // Type = type,
                Note = note
            
            };

            await _context.TranscriptApplications.AddAsync(transcript);
            await _context.SaveChangesAsync();
            return transcript;
        }
        
    }
}