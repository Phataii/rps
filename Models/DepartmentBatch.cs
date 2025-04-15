using System.ComponentModel.DataAnnotations.Schema;

namespace rps.Models
{
    public class DepartmentBatch
    {
        public int Id { get; set;}
        public string? CourseId { get; set;}
        public int Semester { get; set;}
        [ForeignKey("Sessions")]
        public int Session { get; set;}
        public Session? Sessions { get; set;}
        public string? DepartmentName { get; set;}
        public string ResultId { get; set;}
        [ForeignKey("Users")]
        public string? User { get; set;}  // Course Lecturer
        public User? Users { get; set;}
        public int NoOfStudents { get; set;}
        public string? LecturerStatus { get; set;} // Pending - Approved - Rejected (Course Lecturer Approval) 
        public string? HODStatus { get; set;} // Pending - Approved - Rejected (HOD Approval) 
        public string? DeanStatus { get; set;} // Pending - Approved - Rejected (HOD Approval) 
        public DateTime CreatedAt { get; set;}
    } 
}