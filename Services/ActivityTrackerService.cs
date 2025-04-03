using Microsoft.EntityFrameworkCore;
using rps.Data;
using rps.Models;


namespace rps.Services
{
    public class ActivityTrackerService
    {
        private readonly ApplicationDbContext _context;

         public ActivityTrackerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivity(string? userId, string? userName, string action)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                CreatedDate = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}