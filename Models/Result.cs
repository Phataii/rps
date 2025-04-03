using System.ComponentModel.DataAnnotations.Schema;

namespace rps.Models
{
    public class Result
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? ResultId { get; set; } // Primary Key
        public string? StudentId { get; set; } // Mat Number
        public string? StudentName { get; set; } // Mat Number
        public string? CourseId { get; set; } // Course Code
       [ForeignKey("Sessions")]
        public int Session { get; set;}
        public Session Sessions { get; set;} // e.g., 10 id from ais
        public int Semester { get; set; } // 1 or 2
        public int LevelId { get; set; }
        public double CA { get; set; }
        public double Exam { get; set; }
        public double Total { get; set; }
        public string? Grade { get; set; }
        public double Upgrade { get; set; }
        public string? UploadedBy { get; set; }
        public bool IsCO {get; set; } // is carryover
        public DateTime? Created { get; set;}
        public DateTime? UpdatedAt { get; set;}
    }
}
