

namespace rps.Models
{
    public class Order
    {
        public int Id { get; set;}
        public string Email { get; set;}
        public string Type { get; set;}
        public double Amount { get; set;}
        public string RefNo { get; set;}
        public string? Status { get; set;}
        public DateTime CreatedAt { get; set;}
    }
}