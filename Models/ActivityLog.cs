
namespace rps.Models
{
    public class ActivityLog
    {
        public int Id { get; set;}
        public string? UserId { get; set;}
        public string? UserName { get; set;}
        public string? Action { get; set;}
        public DateTime? CreatedDate { get; set; }
    }
}