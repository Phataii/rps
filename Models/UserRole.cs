using System.ComponentModel.DataAnnotations.Schema;

namespace rps.Models
{
    public class UserRole
    {
        public string Id { get; set;} = Guid.NewGuid().ToString();
        [ForeignKey("RoleName")]
        public string? RoleId { get; set;}
        public Roles? RoleName { get; set;}
        [ForeignKey("Users")]
        public string? UserId { get; set;}
        public User? Users { get; set;}
    }
}