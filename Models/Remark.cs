
namespace rps.Models
{
    public class Remark
    {
        public int Id { get; set;}
        public int DepartmentId { get; set;}
        public string? RemarkSlug { get; set;} // A
        public double From { get; set;}
        public double To { get; set;}
        public string? Type { get; set;} // ug - pg - mbbs
        public bool Approved { get; set;}
        public string? ApprovedBy { get; set;} // Email of whoever approved
        public DateTime? CreatedAt { get; set;}
        public DateTime? UpdatedAt { get; set;}
    }
}
