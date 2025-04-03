using System.ComponentModel.DataAnnotations.Schema;

namespace rps.Models
{
    public class FacultyBatch
    {
        public int Id { get; set;}
        public int Semester { get; set;}
        [ForeignKey("Sessions")]
        public int Session { get; set;}
        public Session Sessions { get; set;}
         public int DepartmentId { get; set;}
          public string? DepartmentName { get; set;}
        public int Faculty { get; set;}
        public string? Status { get; set;} // Pending - Approved - Rejected (Dean approval) 
        public string? Registry { get; set;} // Pending - Approved - Rejected (Senate Approval) 
        public DateTime CreatedAt { get; set;}
    } 
}