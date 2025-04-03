using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using rps.Models;
using rps.Data;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using rps.Data.Migrations;
using CsvHelper;
using CsvHelper.Configuration;
using System.Text;
using System.Globalization;
using System.ComponentModel;

namespace rps.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserHelper _userHelper;
        private readonly IMemoryCache _cache;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IHttpClientFactory httpClientFactory, UserHelper userHelper, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _context = context;
            _userHelper = userHelper;
             _cache = cache;
        }
        public async Task<IActionResult> Index()
        {
        
            return View();
        }
    
        [Route("result-upload")]
        public async Task<IActionResult> Upload()
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }
            // API URL and API Key
            string apiUrl = $"https://edouniversity.edu.ng/api/v1/coursesapi?departmentId={loggedInUser.DepartmentId}";
            string apiKey = "";

            // Initialize an HTTP Client
            var client = _httpClientFactory.CreateClient();

            // Add API Key to the 'X-API-Key' header
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            try
            {
                // Make GET request to fetch courses
                var response = await client.GetAsync(apiUrl);

                // Ensure a successful response
                response.EnsureSuccessStatusCode();

                // Read response content
                var content = await response.Content.ReadAsStringAsync();
                
                //Deserialize JSON content if necessary (optional);
                var courses = JsonConvert.DeserializeObject<List<Course>>(content);
                
                var dptBatches = await _context.DepartmentBatches.Where(x => x.User == loggedInUser.Id).ToListAsync();
                var sessions = await _context.Sessions.OrderByDescending(s => s.Id).ToListAsync();
                var model = new PreviewResults
                {
                    DepartmentBatches = dptBatches,
                    Courses = courses,
                    Sessions = sessions
                };
                // Return the combined data to the View
                return View(model);
            }
            catch (HttpRequestException ex)
            {
                // Log or handle error
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View("Error"); // Redirect to an error view if needed
            }
        }
       
        [Route("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            if (loggedInUser.IsActive == false)
            {
                return Forbid("Access Denied.");
            }

            var pendingTranscripts = await _context.TranscriptApplications.Where(x => x.Status == TranscriptStatus.Pending).ToListAsync();
            ViewData["transcripts"]= pendingTranscripts.Count();

            var sessions = await _context.Sessions.OrderByDescending(s => s.Id).ToListAsync();
            var ugGrades = await _context.Grades.Where(x => x.Type == "ug").ToListAsync();
            var pgOrMbbs = await _context.Grades.Where(x => x.Type == "pg").ToListAsync();
            var remarks = await _context.Remarks.Where(x => x.DepartmentId == loggedInUser.DepartmentId).ToListAsync();
            var viewModel =  new Dashboard
            {
                Sessions = sessions,
                UgGrades = ugGrades,
                PgGrades = pgOrMbbs,
                Remarks = remarks
            };
            return View(viewModel);
        }

        [Route("user-roles")]
        public async Task<IActionResult> UserRoles()
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            var userRoles = await _context.UserRoles
            .Include(ur => ur.RoleName)
            .Include(ur => ur.Users).ToListAsync();

            // These two lists are for the modal to add user to role
            var roles = await _context.Roles.ToListAsync();
            var users = await _context.Users.ToListAsync();

            var viewModel = new UserRolesViewModel
            {
                Users = users,
                Roles = roles,
                UserRoles = userRoles
            };

            return View(viewModel);
        }

        [Route("preview-result")]
        public async Task<IActionResult> Preview(int departmentId, int level, int session, int semester, string dptN)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }
            var sessions = await _context.Sessions.ToListAsync();
            ViewData["session"] = sessions;
            ViewData["dpt"] = departmentId;
            ViewData["dptN"] = dptN;
            TempData["semester"] = semester;
            // API URL and API Key
            string apiUrl = $"https://edouniversity.edu.ng/api/v1/courseregistrationsapi/ugstudents?sessionId={session}&departmentId={departmentId}&levelId={level}&semester={semester}";
           string apiKey = "";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            List<RegisteredCoursesList>? registeredCourses = null;

            try
            {
                var response = await client.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                registeredCourses = JsonConvert.DeserializeObject<List<RegisteredCoursesList>>(content);

                if (registeredCourses == null || registeredCourses.Count == 0)
                {
                    ViewData["Message"] = "No registered students found for the selected course.";
                    return View();
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                return View("Error");
            }
            
            // Fetch results & grades in bulk for better performance
            var results = await _context.Results
                .Where(r => r.DepartmentName == dptN && r.LevelId == level && r.Session == session && r.Semester == semester)
                .ToListAsync();

            if (results == null || results.Count == 0)
            {
                ViewData["Message"] = "No results found for the selected filters.";
                return View();
            }
            // Create lookup dictionaries for fast access
            var resultsDict = results.GroupBy(r => (r.StudentId, r.CourseId))
                                    .ToDictionary(g => g.Key, g => g.First());

            var gradesDict = await _context.Grades
                .Where(g => g.DepartmentId == departmentId)
                .ToDictionaryAsync(g => g.GradeName);

            var remarks = await _context.Remarks.Where(r => r.DepartmentId == departmentId).ToListAsync();
            // Process data efficiently
            var studentResults = registeredCourses.Select(student => new StudentResultViewModel
            {
                MatNumber = student.MatNumber,
                StudentName = student.StudentName,
                Courses = student.RegisteredCourses.Select(course =>
                {
                    resultsDict.TryGetValue((student.MatNumber, course.Code), out var courseResult);
                    var gradeName = courseResult?.Grade;
                    gradesDict.TryGetValue(gradeName ?? "", out var departmentGrade);

                    return new CourseResultViewModel
                    {
                        CourseCode = course.Code,
                        CreditUnit = course.CreditUnit,
                        CourseTitle = course.Title,
                        Total = courseResult?.Total ?? 0,
                        GradeName = courseResult?.Grade,
                        GradePoint = departmentGrade?.GradePoint ?? 0
                    };
                }).ToList(),
                TotalCreditUnits = (int)student.RegisteredCourses.Sum(c => c.CreditUnit),
                Remarks = remarks
            }).ToList();

            return View(studentResults);
        }
        
        [Route("mycourse-preview")]
         public async Task<IActionResult> MyCourse([FromQuery] string course, [FromQuery] int session, [FromQuery] string dpt)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }
            
             var results = await _context.Results.Where(x => x.CourseId == course && x.Session == session && x.DepartmentName == dpt).ToListAsync();
             if (results.Any()){
                ViewData["course"] = course;
                ViewData["dpt"] = results.First().DepartmentName;
                 ViewData["count"] = results.Count();
                 ViewData["level"] = results.First().LevelId + "00";
             }
             var sessionName = await _context.Sessions.Where(x => x.Id == session).Select(x => x.Name).FirstOrDefaultAsync();
             ViewData["session"] = sessionName;
            // Send this to the view so we can use it to upgrade result
            // ViewData["course"]= course;
            // ViewData["session"] = session;
            // ViewData["dpt"] = dpt;

            //hide upgrade based on this
            // var res = await _context.DepartmentBatches.Where(x => x.DepartmentId == dpt && x.Session == session && x.CourseId == course)
            // .Select(x => new 
            //     {
            //         lectuer = x.User,
            //         HODStatus = x.HODStatus,
            //         DeanStatus = x.DeanStatus,
            //         LecturerStatus = x.LecturerStatus,
            //         DepartmentName = x.DepartmentName
            //     })
            // .FirstOrDefaultAsync();

            // check if the loggedIn user is the lecturer and the uploader of the course, if yes, then save and use it to display in the view
            // if (res != null && loggedInUser.Id == res.lectuer){
            //     ViewData["isLecturer"] = "Yes";
            // }
            // ViewData["HODStatus"] = res.HODStatus;
            // ViewData["DeanStatus"] = res?.DeanStatus;
            // ViewData["LecturerStatus"] = res?.LecturerStatus;
            // ViewData["dpt"] = res?.DepartmentName;
            // ViewData["course"] = course;
            
           
            return View(results);
        }


        [Route("result-details")]
        public async Task<IActionResult> ResultDetails([FromQuery] string course, [FromQuery] int session, [FromQuery] int dpt)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            var isDean = await _context.UserRoles
                .Where(x => x.UserId == loggedInUser.Id && x.RoleId == "")
                .Select(x => new { x.RoleId, x.RoleName }) // Ensure RoleName is fetched
                .FirstOrDefaultAsync();

            if (isDean != null)
            {
                ViewData["isDean"] = "Yes";
            }
                
            // Send this to the view so we can use it to upgrade result
            ViewData["course"]= course;
            ViewData["session"] = session;
            // ViewData["dpt"] = dpt;

            //hide upgrade based on this
            var res = await _context.DepartmentBatches.Where(x => x.DepartmentId == dpt && x.Session == session && x.CourseId == course)
            .Select(x => new 
                {
                    lectuer = x.User,
                    HODStatus = x.HODStatus,
                    DeanStatus = x.DeanStatus,
                    LecturerStatus = x.LecturerStatus,
                    DepartmentName = x.DepartmentName
                })
            .FirstOrDefaultAsync();

            // check if the loggedIn user is the lecturer and the uploader of the course, if yes, then save and use it to display in the view
            if (res != null && loggedInUser.Id == res.lectuer){
                ViewData["isLecturer"] = "Yes";
            }
            ViewData["HODStatus"] = res.HODStatus;
            ViewData["DeanStatus"] = res?.DeanStatus;
            ViewData["LecturerStatus"] = res?.LecturerStatus;
            ViewData["dpt"] = res?.DepartmentName;
            ViewData["course"] = course;
            
            var results = await _context.Results.Where(x => x.CourseId == course && x.Session == session && x.DepartmentName == res.DepartmentName).ToListAsync();
            return View(results);
        }

        [Route("edit")]
        public async Task<IActionResult> Edit([FromQuery] string course, [FromQuery] int session, [FromQuery] string dpt)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }
    
            ViewData["course"]= course;
            ViewData["session"] = session;

            var res = await _context.DepartmentBatches.Where(x => x.DepartmentName == dpt && x.Session == session && x.CourseId == course)
            .Select(x => new 
                {
                    lectuer = x.User,
                    HODStatus = x.HODStatus,
                    DeanStatus = x.DeanStatus,
                    LecturerStatus = x.LecturerStatus,
                    DepartmentName = x.DepartmentName
                })
            .FirstOrDefaultAsync();

            // check if the loggedIn user is the lecturer and the uploader of the course, if yes, then save and use it to display in the view
            if (res != null && loggedInUser.Id == res.lectuer){
                ViewData["isLecturer"] = "Yes";
            }
            ViewData["HODStatus"] = res.HODStatus;
            ViewData["DeanStatus"] = res?.DeanStatus;
            ViewData["LecturerStatus"] = res?.LecturerStatus;
            ViewData["dpt"] = res?.DepartmentName;
            ViewData["course"] = course;
            
            var results = await _context.Results.Where(x => x.CourseId == course && x.Session == session && x.DepartmentName == dpt).ToListAsync();
            return View(results);
        }

        [Route("faculty-result")]
        public async Task<IActionResult> FacultyResult([FromQuery] string course, [FromQuery] int session)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            var isDean = await _context.UserRoles
                .FirstOrDefaultAsync(x => x.UserId == loggedInUser.Id && x.RoleId == "2c370f7a-13fb-4e60-99ed-8140ac52566a");
            
            if (isDean == null)
            {
                return BadRequest("User is not a dean of a faculty");
            }

            // Fetch all faculty batches first
            var facultyBatches = await _context.FacultyBatches
                .Include(s => s.Sessions)
                .Where(x => x.Faculty == loggedInUser.Faculty)
                .ToListAsync(); // Fetch into memory before grouping

            // Group by DepartmentId to ensure only unique departments are returned
            var uniqueDepartments = facultyBatches
                .GroupBy(x => x.DepartmentId)
                .Select(g => g.First()) // Get one record per department
                .ToList();

            // Dictionary to store the total count of records per department
            var departmentCourseCounts = facultyBatches
                .GroupBy(x => x.DepartmentId)
                .ToDictionary(g => g.Key, g => g.Count());

            ViewData["number"] = uniqueDepartments.Count();

            return View(uniqueDepartments);
        }
   
        [Route("departmental-result")]
        public async Task<IActionResult> DepartmentResult([FromQuery] string? reference)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

           var isDean = await _context.UserRoles
                .Where(x => x.UserId == loggedInUser.Id && x.RoleId == "2c370f7a-13fb-4e60-99ed-8140ac52566a")
                .Select(x => new { x.RoleId, x.RoleName }) // Ensure RoleName is fetched
                .FirstOrDefaultAsync();

            // Fix scope issue - Declare `code` before the if-else conditions
            string? departmentId;
            if (isDean == null)
            {
                departmentId = loggedInUser.DepartmentName; // Use logged-in user's department if not a dean
               
            }
            else
            {
                departmentId = reference; // If a dean, use the provided department code
            }

            var dptBatches = await _context.DepartmentBatches
                .Where(x => x.DepartmentName == departmentId)
                .Include(s => s.Sessions)
                .ToListAsync();
                if (dptBatches.Any())
                {
                    ViewData["DepartmentName"] = dptBatches.First().DepartmentName ?? ""; // Assuming 'DepartmentName' is a field
                }
                else
                {
                    ViewData["DepartmentName"] = "Department Not Found"; // Fallback value
                }
            ViewData["code"] = departmentId;
            return View(dptBatches);
        }
        
        [Route("logs")]
        public async Task<IActionResult> Logs()
        {
            return View(await _context.ActivityLogs.OrderByDescending(c => c.CreatedDate).ToListAsync());
        }

        [Route("roles")]
        public async Task<IActionResult> Roles()
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            var user = await _userHelper.GetLoggedInUser(Request);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            if (user.IsActive == false)
            {
                return Forbid("Access Denied.");
            }

            // Fetch roles and pass user details to the view
            var roles = await _context.Roles.ToListAsync();
            var model = new RolesViewModel
            {
                Roles = roles,
                User = user
            };

            return View(model);
        }

        [Route("settings")]
        public async Task<IActionResult> Settings()
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            var user = await _userHelper.GetLoggedInUser(Request);
            if (user == null)
            {
                return Unauthorized("User not found.");
            }

            if (user.IsActive == false)
            {
                return Forbid("Access Denied.");
            }

           

            return View();
        }


        [Route("users")]
        public async Task<IActionResult> User()
        {
             var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }
            // API URL and API Key
            string apiUrl = $"https://edouniversity.edu.ng/api/v1/staffapi/academic";
            string apiKey = "";

            // Initialize an HTTP Client
            var client = _httpClientFactory.CreateClient();

            // Add API Key to the 'X-API-Key' header
            client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

            try
            {
                // Make GET request to fetch courses
                var response = await client.GetAsync(apiUrl);

                // Ensure a successful response
                response.EnsureSuccessStatusCode();

                // Read response content
                var content = await response.Content.ReadAsStringAsync();
                
                //Deserialize JSON content if necessary (optional);
                var academicStaff = JsonConvert.DeserializeObject<List<Staff>>(content);
                var users = await _context.Users.ToListAsync();
                var model = new UsersVM
                {
                    Staff = academicStaff,
                    Users = users
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching Users.");
                return RedirectToAction("Error", "Home");
            }
            
        }
        // [Route("result-status")]
        // public async Task<IActionResult> Approval()
        // {
        //     // var roles = await _context.Roles.ToListAsync();
        //     return View();
        // }

        // TRANSCRIPT
        [Route("transcripts")]
        public IActionResult Transcript()
        {
            return View();
        }

        [HttpGet("payment-success")]
        public async Task<IActionResult> PaymentSuccess()
        {
            return View();
        }

        [HttpGet("admin/transcripts")]
        public async Task<IActionResult> AdminTranscripts()
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }
            var transcripts = await _context.TranscriptApplications.ToListAsync();
            return View(transcripts);
        }

       [HttpGet("transcript/application")]
        public async Task<IActionResult> TranscriptApplication(string orderId)
        {
            try
            {
                // Fetch the order details using the orderId
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.RefNo == orderId);

                if (order == null)
                {
                    return NotFound("Order not found.");
                }
                ViewData["type"] = order.Type;
                // Render the transcript application page with the order details
                return View("TranscriptApplication", order); // Replace with your view or API response
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error processing transcript application");
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

       [HttpGet("application/progress")]
        public async Task<IActionResult> Progress(int id)
        {
            try
            {
                // Fetch the application by ID
                var application = await _context.TranscriptApplications
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (application == null)
                {
                    return NotFound("Application not found.");
                }

                // Return the progress view with the application data
                return View("Progress", application);
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error fetching application progress.");

                // Return a 500 Internal Server Error with a generic error message
                return StatusCode(500, "An error occurred while fetching application progress. Please try again later.");
            }
        }
        // [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        // public IActionResult Error()
        // {
        //     return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        // }
        
    }
    public class CsvRecord
    {
        public string MatNo { get; set; }
        public string? Name { get; set; }
        public string? Department { get; set; }
        public string CA { get; set; }
        public string Exam { get; set; }
    }
}
