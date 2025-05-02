using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using rps.Data;
using rps.Models;
using rps.Services;

namespace rps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GradeController : Controller
    {
        private readonly GradeService _gradeService;
        private readonly ILogger<GradeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly UserHelper _userHelper;
        private readonly ActivityTrackerService _activityTrackerService;
        private readonly IEmailService _emailService;

        public GradeController(GradeService gradeService, ILogger<GradeController> logger, UserManager<IdentityUser> userManager, UserHelper userHelper, ActivityTrackerService activityTrackerService, IEmailService emailService)
        {
            _gradeService = gradeService;
            _logger = logger;
            _userManager = userManager;
            _userHelper = userHelper;
            _activityTrackerService = activityTrackerService;
            _emailService = emailService;
        }

    
        [HttpPost("upload")]
        public async Task<IActionResult> UploadGrades(IFormFile file, [FromForm] string type)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            if (file == null || type == null)
            {
                return BadRequest("Invalid file or type ID.");
            }

            var result = await _gradeService.UploadGradesFromCsvAsync(file, type, loggedInUser.DepartmentId, loggedInUser.Email);
            if (result.Contains("successfully"))
            {
                // grab logs
                await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"uploaded grade for {loggedInUser.DepartmentName}");
                return Redirect("/dashboard");
            }

            return BadRequest(result);
        }

        [HttpPost("UpdateGrade")]
        public IActionResult UpdateGrade([FromBody] Grade grade)
        {
            var updated = _gradeService.UpdateGrade(grade);
            if (!updated)
                return NotFound(new { message = "Grade not found" });

            return Ok(new { message = "Grade updated successfully" });
        }


        [HttpPost("upload-remark")]
        public async Task<IActionResult> UploadRemarks(IFormFile file)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            if (file == null)
            {
                return BadRequest("Invalid file or type ID.");
            }

            var remark = await _gradeService.UploadRemarkFromCsvAsync(file, loggedInUser.DepartmentId);
            
            // Record activity logs
            //  var log = await _trackerService.AddLogAsync(1, "Added bulk grade data using csv");
            
            if (remark.Contains("successfully"))
            {
                // grab logs
                await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"uploaded remark for {loggedInUser.DepartmentName}");
                TempData["message"] = remark;
                return Redirect("/dashboard");
            }

            return BadRequest(remark);
        }

        // Update a grade
        // [Route("")]
        [HttpPost("status/{departmentId}")]
        public async Task<IActionResult> UpdateGradeStatus([FromForm] bool status)
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
            var departmentId = loggedInUser.DepartmentId;
            var approvedBy = loggedInUser?.Email;
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Invalid request" });

            var updatedGrade = await _gradeService.GradeStatus(departmentId, status, approvedBy);
            if (updatedGrade == null) 
                return NotFound(new { message = "No grades found for the department" });
            
            // grab logs
            await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"updated grade status to {status}");
            // Send email to user
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Views/Home/EmailTemp", "EmailTemplateGrade.html");
            var placeholders = new Dictionary<string, string>
            {
                { "UserName", loggedInUser?.Name },
                { "CurrentYear", DateTime.Now.Year.ToString() }
            };
            await _emailService.SendEmailAsync(loggedInUser?.Email, "Grade Status", templatePath, placeholders, true);
            return Ok(new { message = "Status updated successfully", status });
        }

        // Create a grade
        [HttpPost]
        public async Task<IActionResult> CreateGrade([FromBody] Grade grade)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var createdGrade = await _gradeService.CreateGradeAsync(grade);
            return CreatedAtAction(nameof(GetGradeById), new { id = createdGrade.Id }, createdGrade);
        }

        // Retrieve all grades
        [HttpGet("get-all-grades")]
        public async Task<IActionResult> GetAllGrades()
        {
            var grades = await _gradeService.GetAllGradesAsync();
            return Ok(grades);
        }

        // Retrieve a grade by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGradeById(int id)
        {
            var grade = await _gradeService.GetGradeByIdAsync(id);
            if (grade == null) return NotFound();

            return Ok(grade);
        }

        // Update a grade
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGrade(int id, [FromBody] Grade grade)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updatedGrade = await _gradeService.UpdateGradeAsync(id, grade);
            if (updatedGrade == null) return NotFound();

            return Ok(updatedGrade);
        }

        // Delete a grade
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrade(int id)
        {
            var isDeleted = await _gradeService.DeleteGradeAsync(id);
            if (!isDeleted) return NotFound();

            return NoContent();
        }


        [HttpPost("upload-for-all")]
        public async Task<IActionResult> UploadGradesForAllDpt(IFormFile file, [FromForm] string type)
        {
            var loggedInUser = await _userHelper.GetLoggedInUser(Request);
            if (loggedInUser == null)
            {
                return Redirect("/");
            }

            if (file == null || type == null)
            {
                return BadRequest("Invalid file or type ID.");
            }

            var result = await _gradeService.UploadGradesForDptsFromCsvAsync(file, type, loggedInUser.Email);
            if (result.Contains("successfully"))
            {
                // grab logs
                await _activityTrackerService.LogActivity(loggedInUser.Id, loggedInUser.Email, $"uploaded grade for {loggedInUser.DepartmentName}");
                return Redirect("/dashboard");
            }

            return BadRequest(result);
        }
        
    }
}
