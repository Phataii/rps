
namespace rps.Models
{
    public class User
    {
        public string? Id { get; set;} = Guid.NewGuid().ToString();
        public string? Name { get; set;}
        public string? Email { get; set;}
         public string? DepartmentName { get; set;}
        public int DepartmentId { get; set;}
        public int Faculty { get; set;}
         public int AISID { get; set;}
        public bool? IsActive { get; set; }
    }
}