using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rps.Data;
using rps.Models;
using CsvHelper;
using CsvHelper.Configuration;
using rps.Services;
using System.Text;
using System.Globalization;
using Newtonsoft.Json;

namespace rps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultController : Controller
    {
        private readonly ResultService _resultService;
        private readonly ILogger<ResultController> _logger;
        private readonly UserHelper _userHelper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ActivityTrackerService _activityTrackerService;
        private readonly IEmailService _emailService;
         private readonly ApplicationDbContext _context;
        public ResultController(ResultService resultService, ApplicationDbContext context, ILogger<ResultController> logger, UserHelper userHelper, IHttpClientFactory httpClientFactory, ActivityTrackerService activityTrackerService, IEmailService emailService)
        {
             _logger = logger;
            _resultService = resultService;
            _userHelper = userHelper;
            _httpClientFactory = httpClientFactory;
            _activityTrackerService = activityTrackerService;
            _emailService = emailService;
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadResult(IFormFile file, [FromForm] string courseId, [FromForm] int sessionId, [FromForm] int semesterId, [FromForm] int levelId)
        {
            try
            {
                var loggedInUser = await _userHelper.GetLoggedInUser(Request);
                if (loggedInUser == null){
                    return Redirect("/");
                }

                // Validate input
                if (file == null || file.Length == 0){
                    TempData["error"] = "Invalid file. Please upload a valid file.";
                    return Redirect("/result-upload");
                }
                
                // Check if the result has been uploaded before.
                var resultExists = await _context.DepartmentBatches.FirstOrDefaultAsync(c => c.CourseId == courseId && c.Session == sessionId);
                if(resultExists != null){
                    TempData["error"] = $"Records for {courseId} already exist for this session. Kindly preview and make changes if you have to or contact the ICT for support.";
                    return Redirect("/result-upload");
                }
                if (string.IsNullOrEmpty(courseId) || sessionId <= 0 || semesterId <= 0 || levelId <= 0)
                {
                    TempData["error"] = "Invalid input parameters. Please check the provided IDs.";
                    return Redirect("/result-upload");
                }

                var departmentId = loggedInUser.DepartmentId;
                var facultyId = loggedInUser.Faculty;
                var uploader = loggedInUser.Email;
                var departmentName = loggedInUser.DepartmentName;

                // Process the uploaded file
                var result = await _resultService.UploadResultFromCsvAsync(file, courseId, sessionId, semesterId, levelId, uploader, loggedInUser.Id);

                if (result.Success)
                {
                    TempData["message"] = result.Message;
                    int studentsCount = result.Count;

                    // Save faculty batch
                    await _resultService.SaveFacultyBatch(semesterId, sessionId, departmentId, departmentName, facultyId);

                    // grab logs
                    await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"uploaded result for {courseId}, {sessionId}, {loggedInUser.DepartmentName}");
                
                    return Redirect("/result-upload"); // Redirect to the correct dashboard page
                }
                else
                {
                    TempData["error"] = result.Message;
                    return Redirect("/result-upload");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading the result.");
                TempData["error"] = "An error occurred while processing your request. Please try again later.";
                return Redirect("/result-upload");
            }
        }

        [HttpPost("add-result")]
        public async Task<IActionResult> AddSingleResult(
            [FromForm] string studentId,
            [FromForm] string studentName,
            [FromForm] string courseId,
            [FromForm] double ca,
            [FromForm] double exam,
            [FromForm] int session,
            [FromForm] int semester,
            [FromForm] string dptN,
            [FromForm] int levelId,
            [FromForm] string reference)
        {
            try
            {
                var loggedInUser = await _userHelper.GetLoggedInUser(Request);
                if (loggedInUser == null)
                {
                    return Redirect("/");
                }

                var resultExist = await _context.Results.FirstOrDefaultAsync(x =>
                    x.StudentId == studentId &&
                    x.CourseId == courseId &&
                    x.Session == session);

                if (resultExist != null)
                {
                    TempData["error"] = $"A record for {studentName} for {courseId} already exists for this session.";
                    return Redirect("/result-upload");
                }

                var uploader = loggedInUser.Email;
                var result = await _resultService.AddSingleResult(
                    studentId, studentName, courseId,
                    session, semester, ca, exam, levelId, uploader, dptN, loggedInUser.DepartmentId, reference);

                if (result.Success)
                {
                    TempData["message"] = result.Message;
                    var updateDptBatchCount = await _resultService.UpdateDptRecord(courseId, session, dptN, 1);
                    await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"Added a record to {courseId} for {studentName}");
                    // await _emailService.SendEmailAsync(loggedInUser.Email, "Added result record", templatePath, placeholders, true);
                    return Redirect(Request.Headers["Referer"].ToString());
                }
                else
                {
                    TempData["error"] = result.Message;
                    return Redirect(Request.Headers["Referer"].ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading the result.");
                TempData["error"] = "An error occurred while processing your request. Please try again later.";
                return Redirect(Request.Headers["Referer"].ToString());
            }
        }

        // [HttpPut("hod/status/{course}/{session}")]
        // For HOD or Dean to approve or reject result
        public async Task<IActionResult> ApproveOrDecline([FromForm] string course, [FromForm] int session, [FromForm] string status, [FromForm] string who, [FromForm] string dpt)
        {
            try
            {
                var loggedInUser = await _userHelper.GetLoggedInUser(Request);
                if (loggedInUser == null)
                {
                    return Redirect("/");
                }

                string result = await _resultService.ApproveOrDecline(course, session, status, who, dpt);

                if (result == "Done")
                {
                    // grab logs
                    await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"changed the status of {course}, in the {session} session to {status}");
                    TempData["message"] = $"Result Status changed to {status} for {course}";

                    // Send email to user after successful upgrade
                    string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views/Home/EmailTemp", "ResultStatus.html");
                    var placeholders = new Dictionary<string, string>
                    {
                       
                        { "course", course },
                        { "status", status },
                        { "who", who},
                        { "CurrentYear", DateTime.Now.Year.ToString() }
                    };

                    await _emailService.SendEmailAsync(loggedInUser.Email, $"Result Status Changed", templatePath, placeholders, true);
                    return Redirect(Request.Headers["Referer"].ToString());
                }
                else
                {
                    TempData["error"] = new { message = result };
                    return Redirect(Request.Headers["Referer"].ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the approval status.");
                return StatusCode(500, "An error occurred while processing your request. Please try again later.");
            }
        }   

        [HttpPost("dean-approval")]
        public async Task<IActionResult> DeanApproveOrDecline([FromForm] int dptId, [FromForm] int session, [FromForm] string status)
        {
            try
            {
                var loggedInUser = await _userHelper.GetLoggedInUser(Request);
                if (loggedInUser == null)
                {
                    return Redirect("/");
                }

                string result = await _resultService.DeanApproveOrDecline(dptId, session, status);

                if (result == "Done")
                {
                     await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"changed the status of {dptId}, in the {session} session to {status}");
                    TempData["message"] = $"Department Status changed to {status}";
                    return Redirect(Request.Headers["Referer"].ToString());
                }
                else
                {
                    return NotFound(new { message = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the approval status.");
                return StatusCode(500, "An error occurred while processing your request. Please try again later.");
            }
        }  
        [HttpPost("upgradeBulkResult")]
        public async Task<IActionResult> UpgradeBulkResult([FromForm] string course, [FromForm] int session, [FromForm] int score)
        {
            try
            {
                var loggedInUser = await _userHelper.GetLoggedInUser(Request);
                if (loggedInUser == null)
                {
                    return Redirect("/");
                }

                string result = await _resultService.UpgradeBulkResult(course, session, score, loggedInUser.DepartmentId);

                //getting the fistname of the lecturer just for the sake of the email template
                string firstName = loggedInUser.Email.Split('@')[0].Split('.')[0];
                string formattedName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(firstName.ToLower());

                if (result == "Done")
                {
                    
                    // Send email to user after successful upgrade
                    string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views/Home/EmailTemp", "EmailTemplateUpgrade.html");
                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", formattedName },
                        { "HOD", loggedInUser.Email },
                        { "Course", course },
                        { "Score", score.ToString() },
                        { "Student", "All Students"},
                        { "CurrentYear", DateTime.Now.Year.ToString() }
                    };

                    await _emailService.SendEmailAsync(loggedInUser.Email, "Result Record Upgrade", templatePath, placeholders, true);

                   return Redirect(Request.Headers["Referer"].ToString());
                }
                else
                {
                    return NotFound(new { message = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the approval status.");
                return StatusCode(500, "An error occurred while processing your request. Please try again later.");
            }
        }  

        [HttpPost("upgradeSingleResult")]
        public async Task<IActionResult> UpgradeSingleResult([FromForm] int id, [FromForm] string studentId, [FromForm] double score, [FromForm] string uploader, [FromForm] string course, [FromForm] int departmentId)
        {
            try
            {
                var loggedInUser = await _userHelper.GetLoggedInUser(Request);
                if (loggedInUser == null)
                {
                    return Redirect("/");
                }

                int dptId = loggedInUser.DepartmentId;
                string result = await _resultService.UpgradeSingleResult(id, studentId, score, departmentId);

                //getting the fistname of the lecturer just for the sake of the email template
                string firstName = uploader.Split('@')[0].Split('.')[0];
                string formattedName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(firstName.ToLower());


                if (result == "Done")
                {
                    // Send email to user after successful upgrade
                    string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views/Home/EmailTemp", "EmailTemplateUpgrade.html");
                    var placeholders = new Dictionary<string, string>
                    {
                        { "UserName", formattedName },
                        { "HOD", loggedInUser.Email },
                        { "Course", course },
                        { "Score", score.ToString() },
                        { "Student", studentId },
                        { "CurrentYear", DateTime.Now.Year.ToString() }
                    };

                    await _emailService.SendEmailAsync(uploader, "Result Record Upgrade", templatePath, placeholders, true);

                    return Redirect(Request.Headers["Referer"].ToString());
                }
                else
                {
                    return NotFound(new { message = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the approval status.");
                return StatusCode(500, "An error occurred while processing your request. Please try again later.");
            }
        }

        [HttpPost("result-temp")]
        public async Task<IActionResult> DownloadCsv([FromForm] int sessionId, [FromForm] string courseCode)
        {
            // Fetch registered students for the selected session and course
            // API URL and API Key
            string encodedCourseCode = Uri.EscapeDataString(courseCode);
            string apiUrl = $"https://edouniversity.edu.ng/api/v1/courseregistrationsapi/ugstudents?sessionId={sessionId}&courseCode={encodedCourseCode}"; 
            string apiKey = Environment.GetEnvironmentVariable("EUI_API_KEY");

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
                    TempData["error"] = "No registered students found for the selected course.";
                    return Redirect("/dashboard");
                }

            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                TempData["error"] = $"An error occurred: {ex.Message}";
               return Redirect("/dashboard");
            }

            if (!registeredCourses.Any())
            {
                TempData["error"] = "No students registered for this course in the selected session.";
                return Redirect("/dashboard");
            }

            // Prepare CSV content
            var csvRecords = registeredCourses
                .Select(s => new CsvRecord 
                { 
                    Department = s.Department.Name, 
                    MatNo = s.MatNumber, 
                    Name = s.Sex?.ToUpper() == "F" ? $"{s.StudentName} (MISS)" : s.StudentName, 
                    CA = "", 
                    Exam = "" 
                })
                .OrderBy(r => r.Department)
                .ToList();
//   var csvRecords = registeredCourses.Select(s => new CsvRecord { Department = s.Department.Name, MatNo = s.MatNumber, Name = s.StudentName,  CA = "", Exam = ""}).OrderBy(r => r.Department).ToList();


            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            // Write CSV headers
            csv.WriteRecords(csvRecords);
            writer.Flush();
            memoryStream.Position = 0;

            // Return CSV file for download
            var fileName = $"{courseCode}_Results_Template.csv";
            return File(memoryStream.ToArray(), "text/csv", fileName);
        }

        [HttpGet("UpdateAllResultGrades")]
        public async Task<IActionResult> UpdateAllResultGrades()
        {
            try
            {
                var loggedInUser = await _userHelper.GetLoggedInUser(Request);
                if (loggedInUser == null)
                {
                    return Redirect("/");
                }

                string result = await _resultService.UpdateAllResultGrades();

                if (result == "Done")
                {
                    await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"Updated all result grades");
                    // TempData["message"] = $"Department Status changed to {status}";
                    return Redirect(Request.Headers["Referer"].ToString());
                }
                else
                {
                    return NotFound(new { message = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the approval status.");
                return StatusCode(500, "An error occurred while processing your request. Please try again later.");
            }
        }

        [HttpDelete("records/{id}")]
        public async Task<IActionResult> DeleteRecords(string id)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }
            var records = await _context.Results.Where(x => x.ResultId == id).ToListAsync();
            var dptBatch = await _context.DepartmentBatches.Where(x => x.ResultId == id).ToListAsync();

            if (records == null || !records.Any())
            {
                return NotFound(new { message = "records not found." });
            }

            _context.Results.RemoveRange(records);

            if (dptBatch == null || !dptBatch.Any())
            {
                return NotFound(new { message = "records not found." });
               
            }
            var course = string.Join(", ", dptBatch.Select(b => b.CourseId));
            _context.DepartmentBatches.RemoveRange(dptBatch);
            await _context.SaveChangesAsync();
            await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"Deleted records for {course}");
            return NoContent();
        }

    }
}