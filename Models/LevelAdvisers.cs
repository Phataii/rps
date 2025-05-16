
namespace rps.Models
{
    public class LevelAdviser
    {
        public string? Id { get; set;} = Guid.NewGuid().ToString();
        public string? StaffId { get; set;}
        public int? Level { get; set;}
        public string? DepartmentName { get; set; }
        public int DepartmentId { get; set;}
        public bool? IsActive { get; set; }
    }
}