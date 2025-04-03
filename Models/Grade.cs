
namespace rps.Models
{
    public class Grade
    {
        public int Id { get; set;}
        public int DepartmentId { get; set;}
        public string? GradeName { get; set;} // A
        public int GradePoint { get; set;} // 5
        public double? MinScore { get; set;}
        public double? MaxScore { get; set;}
        public string? Type { get; set;} // ug - pg - mbbs
        public bool Approved { get; set;}
        public string? ApprovedBy { get; set;} // Email of whoever approved
        public DateTime? CreatedAt { get; set;}
        public DateTime? UpdatedAt { get; set;}
    }
}
