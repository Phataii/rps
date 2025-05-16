namespace rps.Models
{
   public class Course
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? DepartmentId { get; set; }
        public string? Level { get; set; }
        public string? Semester { get; set; }
        public int? CreditUnit { get; set; }
    }

    public class Staff
    {
        public int Id { get; set;}
        public string? Fullname { get; set; }
        public string? SchoolEmail { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int FacultyId { get; set; }
    }
    public class UserRolesViewModel
    {
        public List<User>? Users { get; set; }
        public List<Roles>? Roles { get; set; }
        public List <UserRole>? UserRoles { get; set; }
    }
    public class RolesViewModel
    {
        public List<Roles> Roles { get; set; }
        public User User { get; set; }
    }   

    public class PreviewResults
    {
        public List<DepartmentBatch>? DepartmentBatches { get; set; }
        public List<Course>? Courses{ get; set; }
        public List<Session>? Sessions{ get; set; }
    }
public class CoursesAllocated
{
    public int CourseAllocationId { get; set; }
    public int CourseId { get; set; } // Foreign key to Courses
    public int DepartmentId { get; set; }
    // Add other fields as needed
}

    public class CourseAllocationViewModel
{
    public int CourseAllocationId { get; set; }
    public int CourseId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public int DepartmentId { get; set; }
    // Add other fields as needed
}

    public class Departments
    {
        public int Id { get; set; }
        public string? Name {get; set; }
        public string? ShortCode {get; set; }
        public int FacultyId { get; set; }
    }

    public class Faculty
    {
        public int Id { get; set;}
        public string? Name { get; set; }
    }
    public class UsersVM
    {
        public List<User>? Users { get; set; }
        public List<LevelAdviser>? LevelAdvisers { get; set; }
        public List<Staff>? Staff { get; set; }
        public List<Departments>? Departments { get; set; }
    }

    public class RegisteredCoursesList
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? MatNumber { get; set; }
        public string? Level { get; set; }
        public string? SessionOfRegistration { get; set; }
        public List<Course>? RegisteredCourses { get; set; }
        public Departments? Department { get; set; }
        public string? Sex { get; set; }
    }

    public class StudentResultViewModel
    {
        public string MatNumber { get; set; }
        public string StudentName { get; set; }
        public List<CourseResultViewModel> Courses { get; set; }
        public int TotalCreditUnits { get; set; }
        public List<Remark> Remarks { get; set; }

    }

    public class CourseResultViewModel
    {
        public string? CourseCode { get; set; }
        public string? CourseTitle { get; set; }
        public int? CreditUnit { get; set; }
        public double Total { get; set; } // Assuming Total is a decimal
        public string? GradeName { get; set; }
        public int GradePoint { get; set; }
    }
    public class Dashboard
    {
         public List<Session> Sessions { get; set; }
         public List<Grade> UgGrades { get; set; }
         public List<Grade> PgGrades { get; set; }
         public List<Remark> Remarks{ get; set; }
    }

    public class DeanView
    {
        public List<Result> Results {get; set;}
        public List<Departments> Departments {get; set;}
    }
}